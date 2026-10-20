/**
 * Semantic design tokens.
 *
 * Every color maps to an RGB triplet CSS variable defined in src/styles.css
 * (dark values on :root, light values on html.light). Using
 * <alpha-value> keeps Tailwind opacity modifiers (bg-surface/60) working.
 *
 * Contrast (dark theme, checked against canvas #14100d / surface #1b1613):
 *   ink    #f4ece4 ~15:1   soft  #cbbdb1 ~9:1   muted #b0a094 ~6.6:1
 *   faint  #8a7a6e ~3.8:1  -> decorative / large text only, never body copy
 *   accent-as-text #f28c5c ~6:1, on-accent #1f0f08 on accent ~7:1
 */
module.exports = {
  content: ['./src/**/*.{html,ts}'],
  darkMode: 'class',
  theme: {
    extend: {
      colors: {
        canvas: 'rgb(var(--canvas) / <alpha-value>)',
        surface: 'rgb(var(--surface) / <alpha-value>)',
        raised: 'rgb(var(--raised) / <alpha-value>)',
        line: 'rgb(var(--line) / <alpha-value>)',
        ink: 'rgb(var(--ink) / <alpha-value>)',
        soft: 'rgb(var(--soft) / <alpha-value>)',
        muted: 'rgb(var(--muted) / <alpha-value>)',
        faint: 'rgb(var(--faint) / <alpha-value>)',
        accent: 'rgb(var(--accent) / <alpha-value>)',
        'accent-text': 'rgb(var(--accent-text) / <alpha-value>)',
        'on-accent': 'rgb(var(--on-accent) / <alpha-value>)',
        success: 'rgb(var(--success) / <alpha-value>)',
        warning: 'rgb(var(--warning) / <alpha-value>)',
        danger: 'rgb(var(--danger) / <alpha-value>)',
        violet: 'rgb(var(--violet) / <alpha-value>)'
      },
      fontFamily: {
        display: ["'Space Grotesk'", "'DM Sans'", 'system-ui', 'sans-serif']
      },
      boxShadow: {
        card: '0 18px 44px -24px rgb(0 0 0 / 0.55)',
        'accent-glow': '0 14px 34px -12px rgb(var(--accent) / 0.35)'
      }
    }
  },
  plugins: []
};
