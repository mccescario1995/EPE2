<template>
    <div>
        <video ref="video" width="640" height="480" autoplay></video>
        <div v-if="qrCode">
            <h3>QR Code Data:</h3>
            <p>{{ qrCode }}</p>
        </div>
    </div>
</template>

<script setup>
import { ref, onMounted, watch } from 'vue';

const video = ref(null);
const qrCode = ref(null);

const startCamera = async () => {
    try {
        const stream = await navigator.mediaDevices.getUserMedia({ video: true });
        if (video.value) {
            video.value.srcObject = stream;
            video.value.addEventListener('loadedmetadata', () => {
                video.value.play();
                scanQRCode();
            });
        }
    } catch (err) {
        console.error('Error accessing the camera: ', err);
    }
};

const scanQRCode = () => {
    const canvas = document.createElement('canvas');
    canvas.width = 640;
    canvas.height = 480;
    const context = canvas.getContext('2d');

    const scan = () => {
        if (video.value && video.value.readyState === video.value.HAVE_ENOUGH_DATA) {
            context.drawImage(video.value, 0, 0, canvas.width, canvas.height);

            const imageData = context.getImageData(0, 0, canvas.width, canvas.height);
            // Fetch the QR code data from the Flask API endpoint
            fetch('http://192.168.100.2:5000/video_feed')
                .then(response => response.blob())
                .then(blob => {
                    // Handle the QR code data here
                    const reader = new FileReader();
                    reader.onload = function () {
                        const image = new Image();
                        image.src = reader.result;
                        image.onload = () => {
                            context.drawImage(image, 0, 0);
                            const jsQR = require('jsqr');
                            const code = jsQR(imageData.data, canvas.width, canvas.height);
                            if (code) {
                                qrCode.value = code.data;
                                console.log('QR Code Data:', qrCode.value);
                            }
                        };
                    };
                    reader.readAsDataURL(blob);
                });
        }

        requestAnimationFrame(scan);
    };

    scan();
};

onMounted(() => {
    startCamera();
});
</script>
