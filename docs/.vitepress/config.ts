import { defineConfig } from 'vitepress'

export default defineConfig({
  title: 'NotificationHub',
  description: 'Multi-provider email notification platform — API documentation and guides',
  head: [
    ['link', { rel: 'icon', type: 'image/svg+xml', href: '/logo.svg' }],
  ],
  themeConfig: {
    logo: '/logo.svg',
    siteTitle: 'NotificationHub',
    nav: [
      { text: 'Home', link: 'https://notificationhub.space' },
      { text: 'Features', link: 'https://notificationhub.space/#features' },
      { text: 'Resources', link: 'https://notificationhub.space/#resources' },
      { text: 'Developers', link: '/introduction' },
      { text: 'Log in', link: 'https://notificationhub.space/login' },
    ],
    sidebar: [
      {
        text: 'Getting Started',
        items: [
          { text: 'Introduction', link: '/introduction' },
          { text: 'Quick Start', link: '/quickstart' },
          { text: 'Authentication', link: '/authentication' },
        ],
      },
      {
        text: 'API Reference',
        items: [
          { text: 'Overview', link: '/api/' },
          { text: 'Notifications', link: '/api/notifications' },
          { text: 'Templates', link: '/api/templates' },
          { text: 'Campaigns', link: '/api/campaigns' },
          { text: 'Interactive API Explorer', link: 'https://api.notificationhub.space/docs' },
        ],
      },
      {
        text: 'Guides',
        items: [
          { text: 'Email Templates', link: '/guide/templates' },
          { text: 'Campaigns', link: '/guide/campaigns' },
          { text: 'Multi-Provider Fallback', link: '/guide/providers' },
          { text: 'Webhooks', link: '/webhooks' },
        ],
      },
      {
        text: 'SDKs',
        items: [
          { text: 'TypeScript / JavaScript', link: '/sdk/typescript' },
          { text: 'Python', link: '/sdk/python' },
          { text: 'cURL Examples', link: '/api/curl' },
        ],
      },
      {
        text: 'More',
        items: [
          { text: 'Changelog', link: '/changelog' },
          { text: 'Community Projects', link: '/community' },
          { text: 'Status Page', link: 'https://status.notificationhub.space' },
        ],
      },
    ],
    socialLinks: [
      { icon: 'github', link: 'https://github.com/MuhammedAdebiyi/NotificationHub' },
    ],
    editLink: {
      pattern: 'https://github.com/MuhammedAdebiyi/NotificationHub/edit/main/docs/:path',
      text: 'Edit this page on GitHub',
    },
    footer: {
      message: 'Built with NotificationHub.',
      copyright: '© 2026 NotificationHub',
    },
    search: {
      provider: 'local',
    },
  },
})
