<template>
  <div
    class="min-h-screen bg-linear-to-br from-blue-50 to-indigo-100 flex items-center justify-center p-4 relative overflow-hidden"
  >
    <!-- Animated background elements -->
    <div class="absolute inset-0 overflow-hidden">
      <div
        class="absolute -top-40 -right-40 w-80 h-80 bg-white/10 rounded-full blur-3xl animate-pulse"
      ></div>
      <div
        class="absolute -bottom-40 -left-40 w-80 h-80 bg-white/10 rounded-full blur-3xl animate-pulse delay-1000"
      ></div>
      <div
        class="absolute top-1/2 left-1/2 transform -translate-x-1/2 -translate-y-1/2 w-96 h-96 bg-white/5 rounded-full blur-3xl animate-pulse delay-2000"
      ></div>
    </div>

    <!-- Floating particles -->
    <!-- <div class="absolute inset-0 overflow-hidden pointer-events-none">
      <div v-for="i in 20" :key="i" 
        class="absolute w-2 h-2 bg-red-500 rounded-full animate-pulse"
        :style="{
          left: `${Math.random() * 100}%`,
          top: `${Math.random() * 100}%`,
          animationDelay: `${Math.random() * 3}s`,
          animationDuration: `${2 + Math.random() * 3}s`
        }">
      </div>
    </div> -->

    <!-- Main card -->
    <div class="w-full max-w-md relative z-10">
      <div
        class="bg-white/95 backdrop-blur-xl rounded-3xl shadow-2xl overflow-hidden border border-white/20 transform transition-all duration-500"
      >
        <!-- Header with gradient -->
        <div
          class="relative bg-linear-to-r from-indigo-500 to-purple-500 p-8 pb-12"
        >
          <div class="absolute inset-0 bg-black/10"></div>
          <div class="relative z-10 text-center">
            <!--   <div class="inline-flex items-center justify-center w-16 h-16 bg-white/20 backdrop-blur-sm rounded-2xl mb-4 transform transition-transform hover:rotate-12">
              <svg class="w-8 h-8 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z"/>
              </svg>
            </div> -->
            <h1 class="text-3xl font-bold text-white mb-2">Welcome Back</h1>
            <p class="text-indigo-100">Sign in with your fastgroup account</p>
          </div>
          <div class="absolute -bottom-6 left-0 right-0 flex justify-center">
            <div class="w-12 h-12 bg-white rounded-full shadow-lg"></div>
          </div>
        </div>

        <!-- Form section -->
        <div class="p-8 pt-10 space-y-6">
          <div @submit.prevent="Login" class="space-y-5">
            <!-- Email input -->
            <div class="relative group">
              <label class="block text-sm font-semibold text-gray-700 mb-2">
                Email Address
              </label>
              <div class="relative">
                <div
                  class="absolute inset-y-0 left-0 pl-4 flex items-center pointer-events-none"
                >
                  <svg
                    class="w-5 h-5 transition-colors duration-300"
                    :class="
                      focusedField === 'email'
                        ? 'text-indigo-600'
                        : 'text-gray-400'
                    "
                    fill="none"
                    stroke="currentColor"
                    viewBox="0 0 24 24"
                  >
                    <path
                      stroke-linecap="round"
                      stroke-linejoin="round"
                      stroke-width="2"
                      d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z"
                    />
                  </svg>
                </div>
                <input
                  ref="emailInputRef"
                  type="email"
                  v-model="username"
                  @keypress.enter="Login"
                  @focus="focusedField = 'email'"
                  @blur="focusedField = null"
                  class="w-full pl-12 pr-4 py-3.5 bg-gray-50 border-2 border-gray-200 rounded-xl focus:border-indigo-500 focus:bg-white focus:ring-4 focus:ring-indigo-100 outline-none transition-all duration-300 text-gray-800"
                  placeholder="Enter your email"
                  required
                />
              </div>
            </div>

            <!-- Password input -->
            <div class="relative group">
              <label class="block text-sm font-semibold text-gray-700 mb-2">
                Password
              </label>
              <div class="relative">
                <div
                  class="absolute inset-y-0 left-0 pl-4 flex items-center pointer-events-none"
                >
                  <svg
                    class="w-5 h-5 transition-colors duration-300"
                    :class="
                      focusedField === 'password'
                        ? 'text-indigo-600'
                        : 'text-gray-400'
                    "
                    fill="none"
                    stroke="currentColor"
                    viewBox="0 0 24 24"
                  >
                    <path
                      stroke-linecap="round"
                      stroke-linejoin="round"
                      stroke-width="2"
                      d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z"
                    />
                  </svg>
                </div>
                <input
                  :type="showPassword ? 'text' : 'password'"
                  v-model="password"
                  @focus="focusedField = 'password'"
                  @blur="focusedField = null"
                  @keydown="checkCapsLock"
                  @keypress.enter="Login"
                  class="w-full pl-12 pr-12 py-3.5 bg-gray-50 border-2 border-gray-200 rounded-xl focus:border-indigo-500 focus:bg-white focus:ring-4 focus:ring-indigo-100 outline-none transition-all duration-300 text-gray-800"
                  placeholder="Enter your password"
                  required
                />
                <button
                  type="button"
                  @click="showPassword = !showPassword"
                  class="absolute inset-y-0 right-0 pr-4 flex items-center text-gray-400 hover:text-indigo-600 transition-colors duration-300"
                >
                  <svg
                    v-if="showPassword"
                    class="w-5 h-5"
                    fill="none"
                    stroke="currentColor"
                    viewBox="0 0 24 24"
                  >
                    <path
                      stroke-linecap="round"
                      stroke-linejoin="round"
                      stroke-width="2"
                      d="M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7a9.97 9.97 0 011.563-3.029m5.858.908a3 3 0 114.243 4.243M9.878 9.878l4.242 4.242M9.88 9.88l-3.29-3.29m7.532 7.532l3.29 3.29M3 3l3.59 3.59m0 0A9.953 9.953 0 0112 5c4.478 0 8.268 2.943 9.543 7a10.025 10.025 0 01-4.132 5.411m0 0L21 21"
                    />
                  </svg>
                  <svg
                    v-else
                    class="w-5 h-5"
                    fill="none"
                    stroke="currentColor"
                    viewBox="0 0 24 24"
                  >
                    <path
                      stroke-linecap="round"
                      stroke-linejoin="round"
                      stroke-width="2"
                      d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"
                    />
                    <path
                      stroke-linecap="round"
                      stroke-linejoin="round"
                      stroke-width="2"
                      d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z"
                    />
                  </svg>
                </button>
              </div>
              <div
                v-if="isCapsLockOn"
                class="mt-2 flex items-center text-sm text-amber-600 animate-pulse"
              >
                <svg
                  class="w-4 h-4 mr-1"
                  fill="currentColor"
                  viewBox="0 0 20 20"
                >
                  <path
                    fill-rule="evenodd"
                    d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z"
                    clip-rule="evenodd"
                  />
                </svg>
                Caps Lock is on
              </div>
            </div>

            <!-- Remember me & Forgot password -->
            <div class="flex items-center justify-between">
              <label class="flex items-center cursor-pointer group">
                <input
                  type="checkbox"
                  class="w-4 h-4 text-indigo-600 border-gray-300 rounded focus:ring-indigo-500 focus:ring-2 cursor-pointer"
                />
                <span
                  class="ml-2 text-sm text-gray-700 group-hover:text-indigo-600 transition-colors duration-200"
                >
                  Remember me
                </span>
              </label>
              <button
                type="button"
                @click="forgot"
                class="text-sm font-semibold text-indigo-600 hover:text-indigo-700 transition-colors duration-200"
              >
                Forgot password?
              </button>
            </div>

            <!-- Submit button -->
            <button
              @click="Login"
              :disabled="!loginspinnerindicator"
              class="w-full relative group bg-linear-to-r from-indigo-600 to-purple-600 text-white py-4 rounded-xl font-semibold shadow-lg hover:shadow-xl transform hover:-translate-y-0.5 transition-all duration-300 disabled:opacity-50 disabled:cursor-not-allowed disabled:transform-none overflow-hidden"
            >
              <span
                class="absolute inset-0 bg-linear-to-r from-indigo-700 to-purple-700 opacity-0 group-hover:opacity-100 transition-opacity duration-300"
              ></span>
              <span class="relative flex items-center justify-center">
                <template v-if="loginspinnerindicator">
                  Sign In
                  <svg
                    class="w-5 h-5 ml-2 group-hover:translate-x-1 transition-transform duration-300"
                    fill="none"
                    stroke="currentColor"
                    viewBox="0 0 24 24"
                  >
                    <path
                      stroke-linecap="round"
                      stroke-linejoin="round"
                      stroke-width="2"
                      d="M13 7l5 5m0 0l-5 5m5-5H6"
                    />
                  </svg>
                </template>
                <template v-else>
                  <svg
                    class="animate-spin -ml-1 mr-3 h-5 w-5 text-white"
                    xmlns="http://www.w3.org/2000/svg"
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
                    ></circle>
                    <path
                      class="opacity-75"
                      fill="currentColor"
                      d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
                    ></path>
                  </svg>
                  Signing in...
                </template>
              </span>
            </button>
          </div>

          <!-- Divider -->
          <!-- <div class="relative my-6">
            <div class="absolute inset-0 flex items-center">
              <div class="w-full border-t border-gray-200"></div>
            </div>
            <div class="relative flex justify-center text-sm">
              <span class="px-4 bg-white text-gray-500 font-medium"
                >Or continue with</span
              >
            </div>
          </div> -->

          <!-- Social login buttons -->
          <!-- <div class="grid grid-cols-2 gap-3">
            <button
              type="button"
              class="group flex items-center justify-center py-3 px-4 bg-gray-50 border-2 border-gray-200 rounded-xl hover:bg-gray-100 hover:border-gray-300 transition-all duration-300 hover:shadow-md"
            >
              <svg
                class="w-5 h-5 text-gray-700 group-hover:scale-110 transition-transform duration-300"
                fill="currentColor"
                viewBox="0 0 20 20"
              >
                <path
                  fill-rule="evenodd"
                  d="M10 0C4.477 0 0 4.477 0 10c0 4.42 2.865 8.166 6.839 9.489.5.092.682-.217.682-.482 0-.237-.008-.866-.013-1.7-2.782.603-3.369-1.342-3.369-1.342-.454-1.155-1.11-1.462-1.11-1.462-.908-.62.069-.608.069-.608 1.003.07 1.531 1.03 1.531 1.03.892 1.529 2.341 1.087 2.91.832.092-.647.35-1.088.636-1.338-2.22-.253-4.555-1.11-4.555-4.943 0-1.091.39-1.984 1.029-2.683-.103-.253-.446-1.27.098-2.647 0 0 .84-.269 2.75 1.025A9.564 9.564 0 0110 4.844c.85.004 1.705.114 2.504.336 1.909-1.294 2.747-1.025 2.747-1.025.546 1.377.203 2.394.1 2.647.64.699 1.028 1.592 1.028 2.683 0 3.842-2.339 4.687-4.566 4.933.359.309.678.919.678 1.852 0 1.336-.012 2.415-.012 2.743 0 .267.18.578.688.48C17.14 18.163 20 14.418 20 10c0-5.523-4.477-10-10-10z"
                  clip-rule="evenodd"
                />
              </svg>
              <span class="ml-2 text-sm font-semibold text-gray-700"
                >GitHub</span
              >
            </button>
            <button
              type="button"
              class="group flex items-center justify-center py-3 px-4 bg-gray-50 border-2 border-gray-200 rounded-xl hover:bg-gray-100 hover:border-gray-300 transition-all duration-300 hover:shadow-md"
            >
              <svg
                class="w-5 h-5 text-gray-700 group-hover:scale-110 transition-transform duration-300"
                fill="currentColor"
                viewBox="0 0 20 20"
              >
                <path
                  d="M6.29 18.251c7.547 0 11.675-6.253 11.675-11.675 0-.178 0-.355-.012-.53A8.348 8.348 0 0020 3.92a8.19 8.19 0 01-2.357.646 4.118 4.118 0 001.804-2.27 8.224 8.224 0 01-2.605.996 4.107 4.107 0 00-6.993 3.743 11.65 11.65 0 01-8.457-4.287 4.106 4.106 0 001.27 5.477A4.073 4.073 0 01.8 7.713v.052a4.105 4.105 0 003.292 4.022 4.095 4.095 0 01-1.853.07 4.108 4.108 0 003.834 2.85A8.233 8.233 0 010 16.407a11.616 11.616 0 006.29 1.84"
                />
              </svg>
              <span class="ml-2 text-sm font-semibold text-gray-700"
                >Twitter</span
              >
            </button>
          </div> -->
        </div>

        <!-- Footer -->
        <div
          class="bg-linear-to-r from-gray-50 to-indigo-50 px-8 py-6 text-center border-t border-gray-100"
        >
          <p class="text-sm text-gray-600">
            Don't have an account?
            <a
              href="https://ticket.fastlogistics.com.ph/open.php"
              target="_blank"
              class="font-semibold text-indigo-600 hover:text-indigo-700 transition-colors duration-200"
            >
              Request one here
            </a>
          </p>
        </div>
      </div>

      <!-- Trust indicator -->
      <!-- <div class="mt-6 text-center text-white/80 text-sm">
        <p>🔒 Secure login with 256-bit encryption</p>
      </div> -->
    </div>
  </div>
</template>

<script setup>
definePageMeta({
  layout: "empty",
});

const username = ref("test@fastgroup.biz");
const password = ref("test");
const showPassword = ref(false);
const isCapsLockOn = ref(false);
const loginspinnerindicator = ref(true);
const focusedField = ref(null);
const emailInputRef = ref(null);

const router = useRouter();

const { login } = useAuth();

const checkCapsLock = (event) => {
  isCapsLockOn.value = event.getModifierState("CapsLock");
};

async function Login() {
  loginspinnerindicator.value = false;

  if (!username.value || !password.value) {
    loginspinnerindicator.value = true;
    alert("Please enter both username and password");
    return;
  }

  //Use you own API logic here
  const user = await login(username.value, password.value);

  console.log(user);

  if (user.status === "FAILURE") {
    showToast({
      title: "Login Failed",
      message: user.message,
      type: "error",
    });

    loginspinnerindicator.value = true;

    return;
  }

  if (user.status === "SUCCESS") {
    showToast({
      title: "Login Successful",
      message: "Welcome back!",
      type: "success",
    });
  }

  router.push("/dashboard");
}

function forgot() {
  window.location.href = "https://apps.fastlogistics.com.ph/utility/";
}

onMounted(() => {
  if (emailInputRef.value) {
    emailInputRef.value.focus();
  }
});
</script>

<style scoped>
.delay-1000 {
  animation-delay: 1s;
}

.delay-2000 {
  animation-delay: 2s;
}
</style>
