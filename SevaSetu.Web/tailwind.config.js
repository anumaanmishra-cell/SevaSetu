/** @type {import('tailwindcss').Config} */
export default {
  content: ["./index.html", "./src/**/*.{js,ts,jsx,tsx}"],
  theme: {
    extend: {
      colors: {
        brand: { 50: "#eef8f6", 600: "#0f766e", 700: "#0d5c56", 900: "#134e4a" }
      }
    }
  },
  plugins: []
};
