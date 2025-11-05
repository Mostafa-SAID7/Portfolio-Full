export const environment = {
  production: false,
  apiUrl: 'http://localhost:5000/api',
  appName: 'Portfolio Client (Development)',
  appVersion: '1.0.0-dev',
  defaultLanguage: 'en',
  supportedLanguages: ['en', 'es', 'fr'],
  features: {
    darkMode: true,
    notifications: true,
    analytics: false,
    adminPanel: true,
    debugMode: true
  },
  socialLinks: {
    github: 'https://github.com/yourusername',
    linkedin: 'https://linkedin.com/in/yourusername',
    twitter: 'https://twitter.com/yourusername'
  },
  contact: {
    email: 'hello@yourportfolio.com',
    phone: '+1 (555) 123-4567'
  },
  seo: {
    defaultTitle: 'Portfolio - Full Stack Developer (Dev)',
    defaultDescription: 'Welcome to my portfolio website. Discover my projects, skills, and experience as a full-stack developer.',
    siteUrl: 'http://localhost:4200',
    twitterHandle: '@yourusername'
  },
  logging: {
    level: 'debug',
    enableConsole: true,
    enableRemote: false
  },
  cache: {
    enabled: false,
    ttl: 0
  }
};