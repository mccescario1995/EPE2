import { defineStore } from "pinia";
import { ref } from "vue";
import { apiFetch } from "~/composables/useApiFetch";
const userStore = useUserStore();
const userId = userStore.profile;

export const useNotificationStore = defineStore("notification", () => {
  const notifications = ref<Array<any>>([]);
  const unreadCount = ref(0);

  // Fetch notifications for the current user
  const fetchNotifications = async () => {
    const currentProfile = userStore.profile;
    if (!currentProfile?.userId) return;

    try {
      const response = await apiFetch("/api/notification", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({ UserId: parseInt(currentProfile.userId) }),
        credentials: "include",
        parseJson: true,
      });

      notifications.value = response || [];
      updateUnreadCount();
    } catch (error) {
      console.error("Failed to fetch notifications:", error);
    }
  };

  // Add a new notification (called from backend or frontend)
  const addNotification = (notification: any) => {
    notifications.value.unshift(notification);
    updateUnreadCount();
  };

  // Mark a notification as read
  const markAsRead = (id: string | number) => {
    const notification = notifications.value.find((n) => n.id === id);

    if (notification && !notification.isRead) {
      notification.isRead = 1;
      updateUnreadCount();
    }
  };

  // Mark all notifications as read
  const markAllAsRead = () => {
    notifications.value.forEach((n) => {
      if (!n.isRead) {
        n.isRead = 1; // Assuming backend expects 1 for true
      }
    });
    updateUnreadCount();
  };

  // Update the unread count
  const updateUnreadCount = () => {
    unreadCount.value = notifications.value.filter(
      (n) => n.isRead !== 1,
    ).length;
  };

  // Remove a notification (optional)
  const removeNotification = (id: string | number) => {
    notifications.value = notifications.value.filter((n) => n.id !== id);
    updateUnreadCount();
  };

  const clearNotifications = () => {
    notifications.value = [];
    unreadCount.value = 0;
  };

  return {
    notifications,
    unreadCount,
    fetchNotifications,
    addNotification,
    markAsRead,
    markAllAsRead,
    removeNotification,
    clearNotifications,
  };
});
