<template>
  <form class="bg-white h-full flex flex-col rounded">
    <div class="flex flex-col">
      <div class="overflow-x-auto pb-4">
        <div class="min-w-full inline-block align-middle p-6">
          <div>
            <base-table
              :table-data="datalist"
              tableClass="min-w-full border shadow-md rounded-xl overflow-hidden"
              :actions="[
                {
                  label: IconView,
                  tooltip: 'View',
                  handler: null,
                  class: 'hover:bg-indigo-600',
                  iconClass:
                    'fill-indigo-500 text-indigo-500 group-hover:text-white',
                },
              ]"
              :fieldMappings="{
                status: {
                  0: 'Inactive',
                  1: 'Active',
                  2: 'Approved',
                  3: 'Disapproved',
                  4: 'Resubmit',
                  5: 'Problematic',
                  6: 'Ongoing Approval',
                  7: 'Returned',
                },
              }"
              :fieldColors="{
                status: {
                  0: 'bg-gray-100',
                  1: 'bg-blue-100 text-blue-800',
                  2: 'bg-green-100 text-green-800',
                  3: 'bg-red-100 text-red-800',
                  4: 'bg-orange-100 text-orange-800',
                  6: 'bg-fuchsia-100 text-fuchsia-800',
                  7: 'bg-blue-100 text-blue-800',
                },
              }"
              :badgeFields="['status']"
              :hiddenFields="['corporateId', 'type']"
              :show-search="true"
              :filter="true"
              :per-page="5"
              :pagination="true"
              :total-items="datalist.length"
              :date-fields="['date']"
              :date-format="'MMM dd, yyyy - hh:mm:ss'"
              :filterable-columns="['firstname']"
              :sortable-columns="['status', 'lastname', 'firstname']"
              :showlimitfilter="true"
              :limitfilter="[5,10,20,50,100]"
            />
          </div>
        </div>
      </div>
    </div>
  </form>
</template>

<script setup>
import { IconView } from "#components";

const router = useRouter();
function dashboard() {
  router.push("/main/dashboard");
}

const datalist = [
  {
    firstname: "John",
    lastname: "Doe",
    address: "123 Main St, Springfield, IL",
    contact_number: "+1-555-123-4567",
    email_address: "john.doe@email.com",
    status: 1,
    date: new Date(),
  },
  {
    firstname: "Jane",
    lastname: "Smith",
    address: "456 Oak Rd, Chicago, IL",
    contact_number: "+1-555-234-5678",
    email_address: "jane.smith@email.com",
    status: 0,
    date: new Date(),
  },
  {
    firstname: "Michael",
    lastname: "Johnson",
    address: "789 Pine Ave, Dallas, TX",
    contact_number: "+1-555-345-6789",
    email_address: "michael.johnson@email.com",
    status: 1,
    date: new Date(),
  },
  {
    firstname: "Emily",
    lastname: "Davis",
    address: "101 Maple Ln, Seattle, WA",
    contact_number: "+1-555-456-7890",
    email_address: "emily.davis@email.com",
    status: 1,
    date: new Date(),
  },
  {
    firstname: "Chris",
    lastname: "Brown",
    address: "202 Birch St, San Francisco, CA",
    contact_number: "+1-555-567-8901",
    email_address: "chris.brown@email.com",
    status: 0,
    date: new Date(),
  },
  {
    firstname: "Sarah",
    lastname: "Wilson",
    address: "303 Cedar Blvd, Austin, TX",
    contact_number: "+1-555-678-9012",
    email_address: "sarah.wilson@email.com",
    status: 1,
    date: new Date(),
  },
  {
    firstname: "David",
    lastname: "Taylor",
    address: "404 Elm Dr, Miami, FL",
    contact_number: "+1-555-789-0123",
    email_address: "david.taylor@email.com",
    status: 0,
    date: new Date(),
  },
  {
    firstname: "Linda",
    lastname: "Miller",
    address: "505 Pine St, Denver, CO",
    contact_number: "+1-555-890-1234",
    email_address: "linda.miller@email.com",
    status: 1,
    date: new Date(),
  },
  {
    firstname: "James",
    lastname: "Anderson",
    address: "606 Willow Ave, Boston, MA",
    contact_number: "+1-555-901-2345",
    email_address: "james.anderson@email.com",
    status: 1,
    date: new Date(),
  },
  {
    firstname: "Laura",
    lastname: "Thomas",
    address: "707 Redwood Ln, Portland, OR",
    contact_number: "+1-555-012-3456",
    email_address: "laura.thomas@email.com",
    status: 0,
    date: new Date(),
  },
];
</script>

<style>
.custom-checkbox {
  display: none;
}

.custom-checkbox + .material-symbols--check-rounded {
  display: inline-block;
  width: 1.2em;
  height: 1.2em;
  border: 1px solid gray;
  border-radius: 0.25em;
  position: relative;
  cursor: pointer;
  background-color: transparent;
}

.custom-checkbox:checked + .material-symbols--check-rounded {
  background-color: currentColor;
  background-color: #000000;
  mask-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24'%3E%3Cpath fill='white' d='m9.55 15.15l8.475-8.475q.3-.3.7-.3t.7.3t.3.713t-.3.712l-9.175 9.2q-.3.3-.7.3t-.7-.3L4.55 13q-.3-.3-.288-.712t.313-.713t.713-.3t.712.3z'/%3E%3C/svg%3E");
  mask-size: 100% 100%;
  mask-repeat: no-repeat;
  -webkit-mask-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24'%3E%3Cpath fill='white' d='m9.55 15.15l8.475-8.475q.3-.3.7-.3t.7.3t.3.713t-.3.712l-9.175 9.2q-.3.3-.7.3t-.7-.3L4.55 13q-.3-.3-.288-.712t.313-.713t.713-.3t.712.3z'/%3E%3C/svg%3E");
  -webkit-mask-size: 100% 100%;
  -webkit-mask-repeat: no-repeat;
}
</style>
