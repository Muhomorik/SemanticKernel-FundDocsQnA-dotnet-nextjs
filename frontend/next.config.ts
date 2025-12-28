import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  // Static export for Azure Static Web Apps deployment
  output: "export",

  // Disable image optimization for static export
  images: {
    unoptimized: true,
  },

  // Trailing slash for static hosting compatibility
  trailingSlash: true,
};

export default nextConfig;
