<template>
  <div class="bg-white border shadow dark:bg-gray-700 p-4 rounded-lg mb-3">
    <div class="flex justify-between items-center">
      <div>
        <label class="font-semibold text-2xl text-blue-500">2 </label>
        <label class="font-semibold text-gray-900 dark:text-white"
          >Company is being managed</label
        >
      </div>
      <div class="flex items-center">
        <div class="pr-2">
          <svg
            class="w-6 h-6 fill-gray-800 dark:fill-white"
            xmlns="http://www.w3.org/2000/svg"
            height="32"
            width="28"
            viewBox="0 0 448 512"
          >
            <path
              d="M304 128a80 80 0 1 0 -160 0 80 80 0 1 0 160 0zM96 128a128 128 0 1 1 256 0A128 128 0 1 1 96 128zM49.3 464H398.7c-8.9-63.3-63.3-112-129-112H178.3c-65.7 0-120.1 48.7-129 112zM0 482.3C0 383.8 79.8 304 178.3 304h91.4C368.2 304 448 383.8 448 482.3c0 16.4-13.3 29.7-29.7 29.7H29.7C13.3 512 0 498.7 0 482.3z"
            />
          </svg>
        </div>
        <div class="flex flex-col mr-4">
          <label class="font-semibold text-gray-900 dark:text-white"
            >Sample Name</label
          >
          <label class="font-semibold text-gray-900 dark:text-white"
            >sampleemail@test.com</label
          >
        </div>
        <div>
          <svg
            class="mb-2 dark:fill-white"
            xmlns="http://www.w3.org/2000/svg"
            height="32"
            width="40"
            viewBox="0 0 640 512"
          >
            <path
              d="M224 0a128 128 0 1 1 0 256A128 128 0 1 1 224 0zM178.3 304h91.4c11.8 0 23.4 1.2 34.5 3.3c-2.1 18.5 7.4 35.6 21.8 44.8c-16.6 10.6-26.7 31.6-20 53.3c4 12.9 9.4 25.5 16.4 37.6s15.2 23.1 24.4 33c15.7 16.9 39.6 18.4 57.2 8.7v.9c0 9.2 2.7 18.5 7.9 26.3H29.7C13.3 512 0 498.7 0 482.3C0 383.8 79.8 304 178.3 304zM436 218.2c0-7 4.5-13.3 11.3-14.8c10.5-2.4 21.5-3.7 32.7-3.7s22.2 1.3 32.7 3.7c6.8 1.5 11.3 7.8 11.3 14.8v17.7c0 7.8 4.8 14.8 11.6 18.7c6.8 3.9 15.1 4.5 21.8 .6l13.8-7.9c6.1-3.5 13.7-2.7 18.5 2.4c7.6 8.1 14.3 17.2 20.1 27.2s10.3 20.4 13.5 31c2.1 6.7-1.1 13.7-7.2 17.2l-14.4 8.3c-6.5 3.7-10 10.9-10 18.4s3.5 14.7 10 18.4l14.4 8.3c6.1 3.5 9.2 10.5 7.2 17.2c-3.3 10.6-7.8 21-13.5 31s-12.5 19.1-20.1 27.2c-4.8 5.1-12.5 5.9-18.5 2.4l-13.8-7.9c-6.7-3.9-15.1-3.3-21.8 .6c-6.8 3.9-11.6 10.9-11.6 18.7v17.7c0 7-4.5 13.3-11.3 14.8c-10.5 2.4-21.5 3.7-32.7 3.7s-22.2-1.3-32.7-3.7c-6.8-1.5-11.3-7.8-11.3-14.8V467.8c0-7.9-4.9-14.9-11.7-18.9c-6.8-3.9-15.2-4.5-22-.6l-13.5 7.8c-6.1 3.5-13.7 2.7-18.5-2.4c-7.6-8.1-14.3-17.2-20.1-27.2s-10.3-20.4-13.5-31c-2.1-6.7 1.1-13.7 7.2-17.2l14-8.1c6.5-3.8 10.1-11.1 10.1-18.6s-3.5-14.8-10.1-18.6l-14-8.1c-6.1-3.5-9.2-10.5-7.2-17.2c3.3-10.6 7.7-21 13.5-31s12.5-19.1 20.1-27.2c4.8-5.1 12.4-5.9 18.5-2.4l13.6 7.8c6.8 3.9 15.2 3.3 22-.6c6.9-3.9 11.7-11 11.7-18.9V218.2zm92.1 133.5a48.1 48.1 0 1 0 -96.1 0 48.1 48.1 0 1 0 96.1 0z"
            />
          </svg>
          <button class="border rounded-lg p-1">
            <label class="dark:text-white">Logout</label>
          </button>
        </div>
      </div>
    </div>
  </div>

  <div>
    <div class="flex justify-end mb-5">
      <button class="border rounded-xl p-2 bg-blue-950">
        <label class="text-white">Add Company</label>
      </button>
    </div>
    <div class="bg-white border shadow dark:bg-gray-700 p-4 rounded-lg h-[600px]">
      <div class="flex justify-end">
        <div class="relative">
          <div
            class="absolute inset-y-0 start-0 flex items-center ps-3 pointer-events-none"
          >
            <svg
              class="w-4 h-4 text-gray-500"
              xmlns="http://www.w3.org/2000/svg"
              fill="none"
              viewBox="0 0 20 20"
            >
              <path
                stroke="currentColor"
                stroke-linecap="round"
                stroke-linejoin="round"
                stroke-width="2"
                d="m19 19-4-4m0-7A7 7 0 1 1 1 8a7 7 0 0 1 14 0Z"
              />
            </svg>
          </div>
          <input
            class="p-1 ps-10 rounded-lg border border-black"
            type="text"
            name="search"
            id="search"
            placeholder="Search"
          />
        </div>
      </div>
      <div class="grid gap-6 mb-6 md:grid-cols-4">
        <div class="bg-white border shadow p-4 rounded-lg flex flex-col max-w-[350px]">
          <label class="font-semibold text-lg">Fast Services Corporation</label>
          <div>
            <label class="text-3xl text-blue-500 mr-1 font-bold">1</label>
            <label class="font-semibold text-lg"
              >Facilities being managed</label
            >
          </div>
          <div>
            <label class="text-3xl text-blue-500 mr-1 font-bold">5</label>
            <label class="font-semibold text-lg">Checklist</label>
          </div>
          <hr class="h-px my-3 bg-gray-400 border-0" />
          <div class="flex justify-between">
            <button class="bg-white rounded-lg p-1 hover:bg-slate-500">
              <svg
                class="w-8 fill-gray-800"
                aria-hidden="true"
                xmlns="http://www.w3.org/2000/svg"
                fill="currentColor"
                viewBox="0 0 24 24"
              >
                <path
                  fill-rule="evenodd"
                  d="M9 4a4 4 0 1 0 0 8 4 4 0 0 0 0-8Zm-2 9a4 4 0 0 0-4 4v1c0 1.1.9 2 2 2h8a2 2 0 0 0 2-2v-1a4 4 0 0 0-4-4H7Zm8-1c0-.6.4-1 1-1h1v-1a1 1 0 1 1 2 0v1h1a1 1 0 1 1 0 2h-1v1a1 1 0 1 1-2 0v-1h-1a1 1 0 0 1-1-1Z"
                  clip-rule="evenodd"
                />
              </svg>
            </button>
            <button class="bg-white rounded-lg p-1 hover:bg-slate-500">
              <svg
                class="w-6 h-6 fill-red-500"
                aria-hidden="true"
                xmlns="http://www.w3.org/2000/svg"
                fill="currentColor"
                viewBox="0 0 24 24"
              >
                <path
                  fill-rule="evenodd"
                  d="M8.6 2.6A2 2 0 0 1 10 2h4a2 2 0 0 1 2 2v2h3a1 1 0 1 1 0 2v12a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V8a1 1 0 0 1 0-2h3V4c0-.5.2-1 .6-1.4ZM10 6h4V4h-4v2Zm1 4a1 1 0 1 0-2 0v8a1 1 0 1 0 2 0v-8Zm4 0a1 1 0 1 0-2 0v8a1 1 0 1 0 2 0v-8Z"
                  clip-rule="evenodd"
                />
              </svg>
            </button>
          </div>
        </div>
        <div class="bg-white border shadow p-4 rounded-lg flex flex-col max-w-[350px]">
          <label class="font-semibold text-lg">Fast Services</label>
          <div>
            <label class="text-3xl text-blue-500 mr-1 font-bold">5</label>
            <label class="font-semibold text-lg"
              >Facilities being managed</label
            >
          </div>
          <div>
            <label class="text-3xl text-blue-500 font-bold mr-1">2</label>
            <label class="font-semibold text-lg">Checklist</label>
          </div>
          <hr class="h-px my-3 bg-gray-400 border-0" />
          <div class="flex justify-between">
            <button class="bg-white rounded-lg p-1 hover:bg-slate-500">
              <svg
                class="w-8 fill-gray-800"
                aria-hidden="true"
                xmlns="http://www.w3.org/2000/svg"
                fill="currentColor"
                viewBox="0 0 24 24"
              >
                <path
                  fill-rule="evenodd"
                  d="M9 4a4 4 0 1 0 0 8 4 4 0 0 0 0-8Zm-2 9a4 4 0 0 0-4 4v1c0 1.1.9 2 2 2h8a2 2 0 0 0 2-2v-1a4 4 0 0 0-4-4H7Zm8-1c0-.6.4-1 1-1h1v-1a1 1 0 1 1 2 0v1h1a1 1 0 1 1 0 2h-1v1a1 1 0 1 1-2 0v-1h-1a1 1 0 0 1-1-1Z"
                  clip-rule="evenodd"
                />
              </svg>
            </button>
            <button class="bg-white rounded-lg p-1 hover:bg-slate-500">
              <svg
                class="w-6 h-6 fill-red-500"
                aria-hidden="true"
                xmlns="http://www.w3.org/2000/svg"
                fill="currentColor"
                viewBox="0 0 24 24"
              >
                <path
                  fill-rule="evenodd"
                  d="M8.6 2.6A2 2 0 0 1 10 2h4a2 2 0 0 1 2 2v2h3a1 1 0 1 1 0 2v12a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V8a1 1 0 0 1 0-2h3V4c0-.5.2-1 .6-1.4ZM10 6h4V4h-4v2Zm1 4a1 1 0 1 0-2 0v8a1 1 0 1 0 2 0v-8Zm4 0a1 1 0 1 0-2 0v8a1 1 0 1 0 2 0v-8Z"
                  clip-rule="evenodd"
                />
              </svg>
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup></script>
