<script setup lang="ts">
definePageMeta({
  middleware: "auth",
});

const route = useRoute();
const router = useRouter();
const policyid = route.params.id as string;

const userStore = useUserStore();
const userId = userStore.profile;

const loading = ref(true);
const policy = ref<any>(null);
const approvers = ref<any[]>([]);
const approversLoading = ref(false);
const approversError = ref("");

// Computed property to group main approvers with their proxies
const groupedApprovers = computed(() => {
  const mainApprovers: any[] = [];
  const proxiesMap: Map<number, any[]> = new Map();

  const sorted = [...approvers.value].sort((a, b) => {
    const sortA = a.sequence ?? 0;
    const sortB = b.sequence ?? 0;
    if (sortA !== sortB) return sortA - sortB;
    return (a.linesequence ?? 0) - (b.linesequence ?? 0);
  });

  sorted.forEach((approver) => {
    const isMain = approver.linesequence === 1;
    if (isMain) {
      mainApprovers.push(approver);
      proxiesMap.set(approver.sequence, []);
    } else {
      const mainApprover = mainApprovers.find(
        (m) => m.sequence === approver.sequence,
      );
      if (mainApprover) {
        proxiesMap.get(mainApprover.sequence)?.push(approver);
      }
    }
  });

  return { mainApprovers, proxiesMap };
});

const getProxies = (sequence: number): any[] => {
  return groupedApprovers.value.proxiesMap.get(sequence) || [];
};

const fetchApprovers = async () => {
  approversLoading.value = true;
  approversError.value = "";
  try {
    const response = await apiFetch(
      `/api/Maintenance/policy/${policyid}/approvers`,
      {
        method: "POST",
        body: JSON.stringify({ UserId: userId.userId }),
        headers: {
          "Content-Type": "application/json",
        },
        parseJson: true,
      },
    );
    if (response) {
      approvers.value = (response.items || []).map((item: any) => ({
        id: item.id || item.approverid,
        approverid: item.approverid || "",
        name: item.approvername || "",
        email: item.approveremail || "",
        type: item.approvertype || "",
        sequence: item.sort || 0,
        linesequence: item.linesequence || 0,
      }));
    }
  } catch (err) {
    approversError.value = "Failed to load approvers";
    console.error(err);
  } finally {
    approversLoading.value = false;
  }
};

const fetchPolicy = async () => {
  loading.value = true;
  try {
    const response: any = await apiFetch(
      `/api/Maintenance/policy/${policyid}`,
      {
        method: "GET",
        parseJson: true,
      },
    );
    if (response) {
      policy.value = response.items || null;
    }
  } catch (err) {
    console.error(err);
  } finally {
    loading.value = false;
  }
};

const goBack = () => {
  router.back();
};

onMounted(async () => {
  await fetchPolicy();
  await fetchApprovers();
});
</script>

<template>
  <div
    class="min-h-screen bg-gray-50 dark:bg-gray-900 dark:text-gray-200 overflow-auto"
  >
    <header class="bg-white dark:bg-gray-800 shadow">
      <div
        class="me-auto py-4 px-4 sm:px-6 lg:px-8 flex justify-between items-center"
      >
        <div class="flex items-center">
          <Button
            variant="ghost-primary"
            size="lg"
            icon="left_arrow"
            iconPosition="center"
            customClass="rounded-full"
            class="dark:bg-gray-700 dark:text-gray-200 dark:hover:bg-gray-600"
            @onclick="goBack"
          />
          <h1 class="text-2xl ps-5 font-bold text-gray-900 dark:text-gray-200">
            View Policy
          </h1>
        </div>
      </div>
    </header>

    <main class="max-w-full mx-auto py-6 sm:px-6">
      <div
        v-if="loading"
        class="flex items-center justify-center py-16 text-gray-500 dark:text-gray-400"
      >
        Loading policy...
      </div>

      <template v-else-if="policy">
        <div
          class="grid grid-cols-2 gap-6 bg-white dark:bg-gray-800 rounded-lg p-6 border border-gray-200 dark:border-gray-700"
        >
          <div class="grid grid-cols-2 gap-4">
            <div class="col-span-full">
              <div>
                <label
                  class="text-sm font-medium text-gray-700 dark:text-gray-300"
                >
                  Policy ID
                </label>
                <p
                  class="mt-1 px-3 py-2 bg-gray-50 dark:bg-gray-700 border border-gray-300 dark:border-gray-600 rounded-md text-sm"
                >
                  {{ policy.policyid }}
                </p>
              </div>
              <div class="mt-4">
                <label
                  class="text-sm font-medium text-gray-700 dark:text-gray-300"
                >
                  Short Name
                </label>
                <p
                  class="mt-1 px-3 py-2 bg-gray-50 dark:bg-gray-700 border border-gray-300 dark:border-gray-600 rounded-md text-sm"
                >
                  {{ policy.shortname }}
                </p>
              </div>
            </div>

            <div class="col-span-full">
              <label
                class="text-sm font-medium text-gray-700 dark:text-gray-300"
              >
                Status
              </label>
              <p class="mt-1">
                <span
                  v-if="policy.status === 'A'"
                  class="inline-flex items-center px-2.5 py-0.5 rounded text-xs font-medium bg-green-100 text-green-800 border border-green-200 dark:bg-green-900/20 dark:text-green-300 dark:border-green-800"
                >
                  Active
                </span>
                <span
                  v-else
                  class="inline-flex items-center px-2.5 py-0.5 rounded text-xs font-medium bg-red-100 text-red-800 border border-red-200 dark:bg-red-900/20 dark:text-red-300 dark:border-red-800"
                >
                  Inactive
                </span>
              </p>
            </div>

            <div class="col-span-full">
              <div class="col-span-full">
                <label
                  class="text-sm font-medium text-gray-700 dark:text-gray-300"
                >
                  Created Date
                </label>
                <p
                  class="mt-1 px-3 py-2 bg-gray-50 dark:bg-gray-700 border border-gray-300 dark:border-gray-600 rounded-md text-sm"
                >
                  {{
                    policy.createddate
                      ? new Date(policy.createddate).toLocaleString()
                      : "-"
                  }}
                </p>
              </div>
            </div>

            <div class="col-span-full">
              <label
                class="text-sm font-medium text-gray-700 dark:text-gray-300"
              >
                Description
              </label>
              <base-input
                isTextarea
                :class="
                  'mt-1 bg-gray-50 dark:bg-gray-700 border border-gray-300 dark:border-gray-600'
                "
                disabled
                v-model="policy.description"
              >
              </base-input>
            </div>
          </div>

          <div>
            <div
              class="flex items-start justify-between border-b border-gray-100 dark:border-gray-700 py-3"
            >
              <div>
                <h3
                  class="text-md font-semibold text-gray-900 dark:text-gray-100"
                >
                  Approvers
                </h3>
                <p class="text-xs text-gray-500 dark:text-gray-400 mt-0.5">
                  Approver list for this policy
                </p>
              </div>
            </div>

            <div
              v-if="approversLoading"
              class="flex items-center justify-center py-16"
            >
              <svg
                class="animate-spin h-7 w-7 text-blue-500 dark:text-blue-400"
                fill="none"
                viewBox="0 0 24 24"
              >
                <circle
                  class="opacity-25"
                  cx="12"
                  cy="12"
                  r="10"
                  stroke="currentColor"
                  stroke-width="4"
                />
                <path
                  class="opacity-75"
                  fill="currentColor"
                  d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
                />
              </svg>
              <span class="ml-2 text-sm text-gray-500 dark:text-gray-400"
                >Loading approvers...</span
              >
            </div>

            <div
              v-else-if="!approversLoading && approvers.length === 0"
              class="flex flex-col items-center justify-center py-14 text-center"
            >
              <div
                class="w-14 h-14 bg-blue-50 dark:bg-gray-800/20 rounded-full flex items-center justify-center mb-3"
              >
                <svg
                  class="w-7 h-7 text-blue-400 dark:text-blue-300"
                  fill="none"
                  viewBox="0 0 24 24"
                  stroke="currentColor"
                >
                  <path
                    stroke-linecap="round"
                    stroke-linejoin="round"
                    stroke-width="1.5"
                    d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0z"
                  />
                </svg>
              </div>
              <p class="text-sm font-medium text-gray-700 dark:text-gray-300">
                No approvers yet
              </p>
              <p class="text-xs text-gray-400 dark:text-gray-500 mt-1">
                No approvers configured for this policy
              </p>
            </div>
            <div v-else class="space-y-3">
              <div
                v-for="(mainApprover, index) in groupedApprovers.mainApprovers"
                :key="mainApprover.approverid"
                class="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-600 rounded p-3"
              >
                <div
                  class="font-medium text-gray-900 dark:text-gray-100 flex items-center gap-2"
                >
                  <span
                    class="rounded-full bg-blue-200 dark:bg-blue-800 h-7 w-7 flex justify-center items-center text-xs font-semibold"
                  >
                    {{ index + 1 }}
                  </span>
                  <div>
                    <div class="text-sm w-full">{{ mainApprover.name }}</div>

                    <div class="text-xs text-gray-500 dark:text-gray-400">
                      {{ mainApprover.email }}
                    </div>
                  </div>
                </div>

                <!-- Proxy approvers -->
                <div
                  v-if="getProxies(mainApprover.sequence).length > 0"
                  class="ml-6 mt-2 space-y-1"
                >
                  <div
                    v-for="proxy in getProxies(mainApprover.sequence)"
                    :key="proxy.approverid"
                    class="flex items-center gap-2 text-sm text-gray-600 dark:text-gray-400"
                  >
                    <span
                      class="rounded-full bg-orange-100 dark:bg-orange-600 dark:text-gray-300 h-5 w-5 flex justify-center items-center text-xs font-semibold"
                    >
                      P
                    </span>
                    <div>
                      <div>
                        {{ proxy.name }}
                      </div>
                      <div class="text-xs text-gray-400 dark:text-gray-500">
                        {{ proxy.email }}
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </template>

      <div v-else class="text-center py-16 text-gray-500 dark:text-gray-400">
        Policy not found.
      </div>
    </main>
  </div>
</template>
