"use client";

import { ThemeToggle } from "@/components/theme-toggle";
import { Activity, Network, TrendingUp, type LucideIcon } from "lucide-react";
import Link from "next/link";
import { usePathname } from "next/navigation";
import { useChatContext } from "./chat-context";

interface NavItem {
  href: string;
  label: string;
  icon: LucideIcon;
}

const NAV: NavItem[] = [
  { href: "/", label: "Fund Insights", icon: TrendingUp },
  { href: "/ownership-flow", label: "Ownership Flow", icon: Activity },
  { href: "/fund-clusters", label: "Fund Clusters", icon: Network },
];

function isCurrent(href: string, pathname: string | null): boolean {
  const path = pathname?.replace(/\/$/, "") || "/";
  return href === "/" ? path === "/" : path.startsWith(href);
}

export function Header() {
  const { resetChat } = useChatContext();
  const pathname = usePathname();

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

        <div className="flex items-center gap-1">
          {NAV.filter((item) => !isCurrent(item.href, pathname)).map(
            ({ href, label, icon: Icon }) => (
              <Link
                key={href}
                href={href}
                aria-label={label}
                className="text-muted-foreground hover:text-foreground flex items-center gap-1.5 rounded-lg px-3 py-1.5 text-sm font-medium transition-colors duration-200"
              >
                <Icon className="h-3.5 w-3.5" />
                <span className="hidden sm:inline">{label}</span>
              </Link>
            )
          )}
          <ThemeToggle />
        </div>
      </div>
    </header>
  );
}
