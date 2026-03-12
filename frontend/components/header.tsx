"use client";

import { ThemeToggle } from "@/components/theme-toggle";
import { Activity, ArrowLeft, TrendingUp } from "lucide-react";
import Link from "next/link";
import { usePathname } from "next/navigation";
import { useChatContext } from "./chat-context";

export function Header() {
  const { resetChat } = useChatContext();
  const pathname = usePathname();
  const isOnFlowPage = pathname?.startsWith("/ownership-flow");

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
          className="flex cursor-pointer items-center gap-3 transition-opacity hover:opacity-80"
        >
          <div className="bg-primary/10 flex h-9 w-9 items-center justify-center rounded-xl">
            <TrendingUp className="text-primary h-5 w-5" strokeWidth={1.5} />
          </div>
          <span className="font-serif text-lg font-medium tracking-tight">
            Fund Insights
          </span>
        </button>

        <div className="flex items-center gap-2">
          <Link
            href={isOnFlowPage ? "/" : "/ownership-flow"}
            className="text-muted-foreground hover:text-foreground hidden items-center gap-1.5 rounded-lg px-3 py-1.5 text-sm font-medium transition-colors duration-200 sm:flex"
          >
            {isOnFlowPage ? (
              <>
                <ArrowLeft className="h-3.5 w-3.5" />
                Fund Insights
              </>
            ) : (
              <>
                <Activity className="h-3.5 w-3.5" />
                Ownership Flow
              </>
            )}
          </Link>
          <ThemeToggle />
        </div>
      </div>
    </header>
  );
}
