<template>
  <div class="bg-gray-50 py-12 px-4 sm:px-6 lg:px-8">
    <div class="max-w-3xl mx-auto w-full">
      <div class="text-center mb-12">
        <h1 class="text-3xl font-extrabold text-gray-900 sm:text-4xl">
          Contact Us
        </h1>
        <p class="mt-3 text-xl text-gray-500">
          Fill out the form below and we'll get back to you soon.
        </p>
      </div>

      <div class="bg-white shadow-xl rounded-lg overflow-auto w-full">
        <form @submit.prevent="handleSubmit" class="p-8 sm:p-10">
          <div class="grid grid-cols-1 gap-6 sm:grid-cols-2">
            <!-- First Name -->
            <div>
              <label
                for="first-name"
                class="block text-sm font-medium text-gray-700"
              >
                First name <span class="text-red-500">*</span>
              </label>
              <input
                id="first-name"
                v-model="firstName"
                type="text"
                required
                class="mt-1"
                :class="{ 'border-red-500': errors.firstName }"
              />
              <p v-if="errors.firstName" class="mt-1 text-sm text-red-600">
                {{ errors.firstName }}
              </p>
            </div>

            <!-- Last Name -->
            <div>
              <label
                for="last-name"
                class="block text-sm font-medium text-gray-700"
              >
                Last name <span class="text-red-500">*</span>
              </label>
              <input
                id="last-name"
                v-model="form.lastName"
                type="text"
                required
                class="mt-1"
                :class="{ 'border-red-500': errors.lastName }"
              />
              <p v-if="errors.lastName" class="mt-1 text-sm text-red-600">
                {{ errors.lastName }}
              </p>
            </div>

            <!-- Contact -->
            <div class="sm:col-span-2">
              <label
                for="contact"
                class="block text-sm font-medium text-gray-700"
              >
                Phone number <span class="text-red-500">*</span>
              </label>
              <input
                id="contact"
                v-model="form.contact"
                type="tel"
                required
                class="mt-1"
                :class="{ 'border-red-500': errors.contact }"
              />
              <p v-if="errors.contact" class="mt-1 text-sm text-red-600">
                {{ errors.contact }}
              </p>
            </div>

            <!-- Email -->
            <div class="sm:col-span-2">
              <label
                for="email"
                class="block text-sm font-medium text-gray-700"
              >
                Email <span class="text-red-500">*</span>
              </label>
              <input
                id="email"
                v-model="form.email"
                type="email"
                required
                class="mt-1"
                :class="{ 'border-red-500': errors.email }"
              />
              <p v-if="errors.email" class="mt-1 text-sm text-red-600">
                {{ errors.email }}
              </p>
            </div>

            <!-- Message -->
            <div class="sm:col-span-2">
              <label
                for="message"
                class="block text-sm font-medium text-gray-700"
              >
                Message <span class="text-red-500">*</span>
              </label>
              <textarea
                id="message"
                v-model="form.message"
                rows="6"
                required
                class="mt-1"
                :class="{ 'border-red-500': errors.message }"
              ></textarea>
              <p v-if="errors.message" class="mt-1 text-sm text-red-600">
                {{ errors.message }}
              </p>
            </div>
          </div>

          <!-- Form Actions -->
          <div
            class="mt-8 flex flex-col sm:flex-row sm:justify-between items-center"
          >
            <button
              type="button"
              @click="navigateToDashboard"
              class="inline-flex items-center px-4 py-2 border border-transparent text-sm font-medium rounded-md text-indigo-700 bg-indigo-100 hover:bg-indigo-200 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 mb-4 sm:mb-0"
            >
              <span>
                <svg
                  xmlns="http://www.w3.org/2000/svg"
                  width="1em"
                  height="1em"
                  viewBox="0 0 512 512"
                >
                  <path
                    fill="none"
                    stroke="currentColor"
                    stroke-linecap="round"
                    stroke-linejoin="round"
                    stroke-width="48"
                    d="M244 400L100 256l144-144M120 256h292"
                  />
                </svg>
              </span>
              Back to Dashboard
            </button>

            <button
              type="submit"
              :disabled="isSubmitting"
              class="inline-flex justify-center py-3 px-6 border border-transparent shadow-sm text-sm font-medium rounded-md text-white bg-indigo-600 hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 transition-colors duration-200 disabled:opacity-50 disabled:cursor-not-allowed"
            >
              <template
                v-if="isSubmitting"
                class="flex items-center justify-start"
              >
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
                Processing...
              </template>
              <template v-else> Send Message </template>
            </button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref } from "vue";
import { useRouter } from "vue-router";

const router = useRouter();

const firstName = useState("firstname", () => "");

const form = ref({
  firstName: "",
  lastName: "",
  contact: "",
  email: "",
  message: "",
});

const errors = ref({
  firstName: "",
  lastName: "",
  contact: "",
  email: "",
  message: "",
});

const isSubmitting = ref(false);

const validateForm = () => {
  let isValid = true;
  const newErrors = {
    firstName: "",
    lastName: "",
    contact: "",
    email: "",
    message: "",
  };

  if (!form.value.firstName.trim()) {
    newErrors.firstName = "First name is required";
    isValid = false;
  }

  if (!form.value.lastName.trim()) {
    newErrors.lastName = "Last name is required";
    isValid = false;
  }

  if (!form.value.contact.trim()) {
    newErrors.contact = "Phone number is required";
    isValid = false;
  } else if (!/^[\d\s\+\-\(\)]{10,}$/.test(form.value.contact)) {
    newErrors.contact = "Please enter a valid phone number";
    isValid = false;
  }

  if (!form.value.email.trim()) {
    newErrors.email = "Email is required";
    isValid = false;
  } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(form.value.email)) {
    newErrors.email = "Please enter a valid email address";
    isValid = false;
  }

  if (!form.value.message.trim()) {
    newErrors.message = "Message is required";
    isValid = false;
  } else if (form.value.message.trim().length < 10) {
    newErrors.message = "Message should be at least 10 characters";
    isValid = false;
  }

  errors.value = newErrors;
  return isValid;
};

const handleSubmit = async () => {
  if (!validateForm()) return;

  isSubmitting.value = true;

  try {
    // Simulate API call
    await new Promise((resolve) => setTimeout(resolve, 1500));

    // Reset form on success
    form.value = {
      firstName: "",
      lastName: "",
      contact: "",
      email: "",
      message: "",
    };

    alert("Your message has been sent successfully!");
  } catch (error) {
    console.error("Error submitting form:", error);
    alert("There was an error submitting your form. Please try again.");
  } finally {
    isSubmitting.value = false;
  }
};

const navigateToDashboard = () => {
  router.push("/main/dashboard");
};
</script>

<style scoped>
@import "tailwindcss";
input,
textarea {
  @apply block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm py-3 px-4 border;
}
/* Custom transitions */
.enter-active-class,
.leave-active-class {
  transition: all 0.3s ease;
}

.enter-from-class,
.leave-to-class {
  opacity: 0;
  transform: translateY(-10px);
}
</style>
