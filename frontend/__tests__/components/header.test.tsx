import { render, screen } from "@testing-library/react";
import { Header } from "@/components/header";
import { usePathname } from "next/navigation";

jest.mock("next/navigation", () => ({ usePathname: jest.fn() }));
jest.mock("@/components/chat-context", () => ({
  useChatContext: () => ({ resetChat: jest.fn() }),
}));
jest.mock("@/components/theme-toggle", () => ({
  ThemeToggle: () => null,
}));

const mockPathname = usePathname as jest.Mock;

function navHrefs(): string[] {
  return screen
    .getAllByRole("link")
    .map((link) => link.getAttribute("href") ?? "");
}

describe("Header navigation", () => {
  it.each([
    ["/", ["/ownership-flow", "/fund-clusters"]],
    ["/ownership-flow", ["/", "/fund-clusters"]],
    ["/ownership-flow/", ["/", "/fund-clusters"]],
    ["/fund-clusters", ["/", "/ownership-flow"]],
    ["/fund-clusters/", ["/", "/ownership-flow"]],
  ])("on %s links to %j", (pathname, expected) => {
    mockPathname.mockReturnValue(pathname);

    render(<Header />);

    expect(navHrefs()).toEqual(expected);
  });

  it("labels the links with page names", () => {
    mockPathname.mockReturnValue("/");

    render(<Header />);

    expect(
      screen.getByRole("link", { name: /ownership flow/i })
    ).toBeInTheDocument();
    expect(
      screen.getByRole("link", { name: /fund clusters/i })
    ).toBeInTheDocument();
  });
});
