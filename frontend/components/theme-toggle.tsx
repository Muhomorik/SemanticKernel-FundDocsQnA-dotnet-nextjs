"use client";

import * as React from "react";
import { Moon, Sun } from "lucide-react";
import { useTheme } from "next-themes";
import { Button } from "@/components/ui/button";

export function ThemeToggle() {
  const { theme, setTheme } = useTheme();
  const [mounted, setMounted] = React.useState(false);

  React.useEffect(() => {
    setMounted(true);
  }, []);

  if (!mounted) {
    return (
      <Button
        variant="ghost"
        size="icon"
        disabled
        className="relative h-9 w-9 rounded-xl"
      >
        <Sun className="h-4.5 w-4.5" strokeWidth={1.5} />
        <span className="sr-only">Toggle theme</span>
      </Button>
    );
  }

  const isDark = theme === "dark";

  return (
    <Button
      variant="ghost"
      size="icon"
      onClick={() => setTheme(isDark ? "light" : "dark")}
      className="hover:bg-accent relative h-9 w-9 rounded-xl transition-colors duration-200"
    >
      <Sun
        className="h-4.5 w-4.5 scale-100 rotate-0 transition-all duration-300 ease-out dark:scale-0 dark:-rotate-90"
        strokeWidth={1.5}
      />
      <Moon
        className="absolute h-4.5 w-4.5 scale-0 rotate-90 transition-all duration-300 ease-out dark:scale-100 dark:rotate-0"
        strokeWidth={1.5}
      />
      <span className="sr-only">Toggle theme</span>
    </Button>
  );
}
