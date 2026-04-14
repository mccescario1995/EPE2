// config.js

const config = useRuntimeConfig()
// const isProd = process.env.NODE_ENV === 'production';

export const API_BASE_URL = config.public.apiBase;