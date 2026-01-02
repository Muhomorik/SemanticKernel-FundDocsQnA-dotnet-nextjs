"use client";

import { ThemeToggle } from "@/components/theme-toggle";
import { FileText } from "lucide-react";
import { useChatContext } from "./chat-context";

export function Header() {
  const { resetChat } = useChatContext();

  const handleHeaderClick = () => {
    resetChat();
  };

  return (
    <header className="sticky top-0 z-50 w-full">
      <div className="bg-background/80 absolute inset-0 backdrop-blur-xl" />
      <div className="border-border/50 absolute inset-x-0 bottom-0 h-px bg-linear-to-r from-transparent via-current to-transparent opacity-20" />
      <div className="relative mx-auto flex h-16 max-w-5xl items-center justify-between px-6">
        <button
          onClick={handleHeaderClick}
          className="flex items-center gap-3 transition-opacity hover:opacity-80 cursor-pointer"
        >
          <div className="bg-primary/10 flex h-9 w-9 items-center justify-center rounded-xl">
            <FileText className="text-primary h-5 w-5" strokeWidth={1.5} />
          </div>
          <span className="font-serif text-lg font-medium tracking-tight">
            PDF Q&A
          </span>
        </button>
        <ThemeToggle />
      </div>
    </header>
  );
}
