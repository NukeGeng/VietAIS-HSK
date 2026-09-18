import { defineStore } from 'pinia'

export const useUiStore = defineStore('ui', {
  state: () => ({
    mobileDrawerOpen: false,
    expandedMenus: {
      'Kỹ năng': true,
      'Luyện tập': true,
      'Chữ Hán': false,
    } as Record<string, boolean>,
  }),
  actions: {
    toggleMenu(label: string) {
      this.expandedMenus[label] = !this.expandedMenus[label]
    },
    closeMobileDrawer() {
      this.mobileDrawerOpen = false
    },
  },
})
