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
    try {
      const response = await apiFetch("/api/notification", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({ UserId: parseInt(userId.userId) }),
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

    console.log("Marking notification as read:", notification);
    if (notification && !notification.isRead) {
      notification.isRead = 1; 
      console.log("Notification marked as read:", notification);
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
    unreadCount.value = notifications.value.filter((n) => n.isRead !== 1).length;
  };

  // Remove a notification (optional)
  const removeNotification = (id: string | number) => {
    notifications.value = notifications.value.filter((n) => n.id !== id);
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
