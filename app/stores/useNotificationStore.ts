import { defineStore } from "pinia";
import { ref } from "vue";
import { apiFetch } from "~/composables/useApiFetch";

export const useNotificationStore = defineStore("notification", () => {
  const notifications = ref<Array<any>>([]);
  const unreadCount = ref(0);

  // Fetch notifications for the current user
  const fetchNotifications = async () => {
    try {
      const response = await apiFetch("/api/notifications");
      notifications.value = response.data || [];
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
    const notification = notifications.value.find(n => n.id === id);
    if (notification && !notification.read) {
      notification.read = true;
      updateUnreadCount();
    }
  };

  // Mark all notifications as read
  const markAllAsRead = () => {
    notifications.value.forEach(n => {
      if (!n.read) {
        n.read = true;
      }
    });
    updateUnreadCount();
  };

  // Update the unread count
  const updateUnreadCount = () => {
    unreadCount.value = notifications.value.filter(n => !n.read).length;
  };

  // Remove a notification (optional)
  const removeNotification = (id: string | number) => {
    notifications.value = notifications.value.filter(n => n.id !== id);
    updateUnreadCount();
  };

  return {
    notifications,
    unreadCount,
    fetchNotifications,
    addNotification,
    markAsRead,
    markAllAsRead,
    removeNotification,
  };
});