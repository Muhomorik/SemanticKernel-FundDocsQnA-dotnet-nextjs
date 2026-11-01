import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { useTheme } from "next-themes";
import { ThemeToggle } from "@/components/theme-toggle";

jest.mock("next-themes", () => ({
  useTheme: jest.fn(),
}));

const mockedUseTheme = useTheme as jest.MockedFunction<typeof useTheme>;

describe("ThemeToggle", () => {
  const setTheme = jest.fn();

  const mockTheme = (theme: string) => {
    mockedUseTheme.mockReturnValue({
      theme,
      setTheme,
      themes: ["light", "dark"],
    });
  };

  afterEach(() => {
    jest.resetAllMocks();
  });

  it("renders an enabled toggle once mounted in the browser", () => {
    mockTheme("light");

    render(<ThemeToggle />);

    expect(screen.getByRole("button", { name: "Toggle theme" })).toBeEnabled();
  });

  it("switches to dark when the current theme is light", async () => {
    mockTheme("light");
    render(<ThemeToggle />);

    await userEvent.click(screen.getByRole("button", { name: "Toggle theme" }));

    expect(setTheme).toHaveBeenCalledWith("dark");
  });

  it("switches to light when the current theme is dark", async () => {
    mockTheme("dark");
    render(<ThemeToggle />);

    await userEvent.click(screen.getByRole("button", { name: "Toggle theme" }));

    expect(setTheme).toHaveBeenCalledWith("light");
  });
});
