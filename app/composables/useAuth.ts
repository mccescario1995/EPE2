// composables/useAuth.ts
import { ref } from "vue";
import { useRouter } from "vue-router";
export const accessToken = ref<string | null>(null);
export const user = ref<any>(null);

export function useAuth() {
  // Access Pinia user store
  const userStore = useUserStore();

  async function login(email: string, password: string) {

    const payload = {
      email: email,
      password: password,
    };

    const data = await apiFetch("/api/Access", {
      method: "POST",
      body: JSON.stringify(payload),
      headers: {
        "Content-Type": "application/json",
      },
    });

    //Parse response (required)
    const dataJson = await data.json();

    //Check for login failure
    if (dataJson.status === "FAILURE")
      return { status: dataJson.status, message: dataJson.message };

    accessToken.value = dataJson.stringParam1;

    //Store user data in Pinia store
    userStore.setUser(dataJson.stringParam1, dataJson.profile); // Adjust based on your API response structure (old = dataJson.objectParam1 , new = dataJson.profile)

    return { status: "SUCCESS" };
  }

  //Use it to refresh token (if needed)
  async function refresh() {
    const response = await apiFetch("/api/auth/refresh", {
      method: "POST",
    });

    //Parse response (required)
    const dataJson = await response.json();

    if (dataJson.status === "FAILURE") throw new Error(dataJson.message);

    accessToken.value = dataJson.token;

    userStore.setToken(dataJson.token);

    return accessToken.value;
  }

  async function logout() {
    accessToken.value = null;
    user.value = null;
  }

  return { login, logout, refresh, accessToken, user };
}
