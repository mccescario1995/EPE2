<template>
   <nav class="flex mb-5 h-13 bg-gray-100 rounded-lg" aria-label="Breadcrumb">
        <ul class="p-4 flex flex-wrap space-x-3 text-sm font-medium">
            <li class="flex items-center space-x-3">
                <a @click="dashboard()" class="cursor-pointer flex items-center space-x-1 text-gray-800">
                    <svg class="h-4 w-4 shrink-0 fill-gray-500" aria-hidden="true" viewBox="0 0 256 256">
                        <path
                            d="M184 32H72A16 16 0 0056 48V224a8.1 8.1.0 004.1 7 7.6 7.6.0 003.9 1 7.9 7.9.0 004.2-1.2L128 193.4l59.7 37.4a8.3 8.3.0 008.2.2 8.1 8.1.0 004.1-7V48A16 16 0 00184 32z">
                        </path>
                    </svg>
                    <span>Dashboard</span>
                </a>
            </li>
            <li class="flex items-center space-x-3" aria-current="page">
                <div aria-hidden="true" class="h-4 w-px rotate-12 rounded-full bg-gray-300"></div>

                <span class="flex items-center space-x-1">
                    <svg class="h-4 w-4 shrink-0 fill-gray-400" aria-hidden="true" viewBox="0 0 256 256">
                        <path
                            d="M136 88H120V35.3L91.7 63.6A8 8 0 0180.3 52.3l42-42a8.1 8.1.0 0111.4.0l42 42a8 8 0 010 11.3 8 8 0 01-11.4.0L136 35.3zm64 0H136v40a8 8 0 01-16 0V88H56a16 16 0 00-16 16V208a16 16 0 0016 16H2e2a16 16 0 0016-16V104A16 16 0 002e2 88z">
                        </path>
                    </svg>
                    <span class="text-gray-400">Buttons</span>
                </span>
            </li>
        </ul>
    </nav>
    <div class=" items-center bg-white h-full flex flex-col rounded py-6 sm:py-8 lg:py-12">
        <!-- <div class="mb-4">
            <button
                class="mx-2 px-2 py-1 text-xs rounded text-gray-800 border focus:outline-none hover:bg-gray-100">Default</button>
            <button
                class="mx-2 px-4 py-2 text-sm rounded text-gray-800 border focus:outline-none hover:bg-gray-100">Default</button>
            <button
                class="mx-2 my-2 px-8 py-4 text rounded text-gray-800 border focus:outline-none hover:bg-gray-100">Default</button>
        </div> -->
        <!-- <div class="mb-4">
            <button
                class="mx-2 my-2 px-8 py-2 text rounded text-white bg-blue-500 focus:outline-none hover:bg-blue-300">
            <div class="rounded-xl h-12 w-12 animate-spin rounded-full border-4 border-t-4 border-white-500"/>
            </button>
        </div><br><br> -->

        <div class="mb-1">
         <div class="rounded-xl h-12 w-12 animate-spin rounded-full border-4 border-t-4 border-blue-500"/>
        </div><br><br>

        <div class="mb-4">
          <div class="border-gray-300 h-6 w-6 animate-spin rounded-full border-8 border-t-blue-600" />
        </div><br><br>

        <div class="mb-4">
            <div class="border-gray-300 h-12 w-12 animate-spin rounded-full border-8 border-t-blue-600" />
        </div><br><br>

         <div class="mb-4">
          <div class="border-gray-300 h-16 w-16 animate-spin rounded-full border-8 border-t-blue-600" />
        </div><br><br>

        <div class="mb-4">
            <div class="border-gray-300 h-20 w-20 animate-spin rounded-full border-8 border-t-blue-600" />
        </div>
        
        <!-- <div class="mb-4">
            <button
                class="mx-2 px-2 py-1 text-sx rounded text-gray-800 border focus:outline-none hover:bg-gray-100 inline-flex items-center justify-between">
                Upload file

                <svg class="w-4 h-4 fill-current ml-4" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20">
                    <path d="M13 10v6H7v-6H2l8-8 8 8h-5zM0 18h20v2H0v-2z" />
                </svg>
            </button>
            <button
                class="mx-2 px-4 py-2 text-sm rounded text-gray-800 border focus:outline-none hover:bg-gray-100 inline-flex items-center justify-between">
                Upload file

                <svg class="w-4 h-4 fill-current ml-4" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20">
                    <path d="M13 10v6H7v-6H2l8-8 8 8h-5zM0 18h20v2H0v-2z" />
                </svg>
            </button>
            <button
                class="mx-2 px-2 py-4 text-sm rounded text-gray-800 border focus:outline-none hover:bg-gray-100 inline-flex items-center justify-between">
                Upload file

                <svg class="w-4 h-4 fill-current ml-4" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20">
                    <path d="M13 10v6H7v-6H2l8-8 8 8h-5zM0 18h20v2H0v-2z" />
                </svg>
            </button>
        </div> -->
    </div>
</template>

<script>

</script>

<style></style>