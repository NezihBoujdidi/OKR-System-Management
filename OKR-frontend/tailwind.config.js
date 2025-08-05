/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./src/**/*.{html,ts}",
  ],
  theme: {
    extend: {
      colors: {
        primary: '#1a1a1a',    // Black
        accent: '#FFD700',     // Yellow
        white: '#ffffff',
        'accent-light': 'rgba(255, 215, 0, 0.1)',
        'accent-hover': 'rgba(255, 215, 0, 0.9)',
        // Task highlight colors
        'task-highlight': 'rgba(16, 185, 129, 0.05)',  // Very light green
        'task-highlight-hover': 'rgba(16, 185, 129, 0.1)',  // Light green for hover
        'task-highlight-border': 'rgb(16, 185, 129)',  // Green border
      },
      fontFamily: {
        sans: ['Inter', 'system-ui', '-apple-system', 'sans-serif'],
      },
      keyframes: {
        fadeIn: {
          '0%': { opacity: '0', transform: 'translateY(-10px)' },
          '100%': { opacity: '1', transform: 'translateY(0)' }
        },
        'spin-fast': {
          '0%': { transform: 'rotate(0deg)' },
          '100%': { transform: 'rotate(360deg)' }
        },
        'spin-reverse': {
          'from': { transform: 'rotate(360deg)' },
          'to': { transform: 'rotate(0deg)' },
        },
        'bounce-custom': {
          '0%, 100%': { transform: 'translateY(0)', opacity: '0.4' },
          '50%': { transform: 'translateY(-12px)', opacity: '1' },
        },
        'float': {
          '0%, 100%': { transform: 'translateY(0)' },
          '50%': { transform: 'translateY(-10px)' },
        },
        'float-particle': {
          '0%, 100%': { transform: 'translate(0, 0)', opacity: '0.3' },
          '50%': { transform: 'translate(10px, -10px)', opacity: '1' },
        },
      },
      animation: {
        fadeIn: 'fadeIn 0.5s ease-out',
        'spin-fast': 'spin-fast 0.8s linear infinite',
        'pulse': 'pulse 2s cubic-bezier(0.4, 0, 0.6, 1) infinite',
        'spin-slow': 'spin 4s linear infinite',
        'spin-reverse': 'spin-reverse 3s linear infinite',
        'pulse-slow': 'pulse 3s cubic-bezier(0.4, 0, 0.6, 1) infinite',
        'bounce-custom': 'bounce-custom 1.2s infinite',
        'float': 'float 3s ease-in-out infinite',
        'float-particle': 'float-particle 3s ease-in-out infinite',
      }
    },
  },
  plugins: [],
}