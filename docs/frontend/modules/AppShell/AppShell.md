# AppShell Frontend Module

## Mục đích

Cung cấp shell dùng chung cho learner app: header, sidebar, HSK context selector, mobile drawer và layout container.

## Sidebar

Phải bám đúng `docs/product/00-navigation-and-learning-flow.md`.

Không duplicate logic sidebar theo từng page.

## HSK selector

Hiển thị HSK context hiện tại gần đầu sidebar/header.

Thay đổi context ảnh hưởng các page HSK-scoped nhưng không làm mất beginner progress.

## Components

- AppSidebar;
- SidebarGroup;
- SidebarItem;
- SidebarSubmenu;
- AppHeader;
- HskContextSelector;
- MobileNavigationDrawer;
- AccountMenu.

## State

- sidebar expanded/collapsed;
- submenu expanded;
- mobile drawer;
- selected/preferred HSK context;
- current route active state.

## Responsive

Desktop: sidebar persistent/sticky.

Mobile: drawer; active route và submenu vẫn phải rõ.

## Computer Use tests

- [ ] desktop sidebar đúng group/order;
- [ ] active parent + child đúng;
- [ ] mobile drawer mở/đóng;
- [ ] HSK selector không phá route;
- [ ] refresh giữ active route;
- [ ] không có text clipping tiếng Việt.
