import { render, screen } from "@testing-library/react";
import Home from "@/app/page";
import { ChatProvider } from "@/components/chat-context";

describe("Home Page", () => {
  const renderWithProvider = (component: React.ReactElement) => {
    return render(<ChatProvider>{component}</ChatProvider>);
  };

  it("renders the main heading", () => {
    renderWithProvider(<Home />);
    const heading = screen.getByRole("heading", {
      name: /Ask about your documents/i,
    });
    expect(heading).toBeInTheDocument();
  });

  it("displays the description text", () => {
    renderWithProvider(<Home />);
    const description = screen.getByText(
      /Get AI-powered answers from your pre-processed PDF documents/i
    );
    expect(description).toBeInTheDocument();
  });

  it("shows example query groups and hint", () => {
    renderWithProvider(<Home />);

    // Check for group buttons (new hierarchical structure)
    const groups = [
      "Quick Start",
      "Comparison Questions",
      "Specific Funds",
      "Single Fund Questions",
    ];

    groups.forEach((group) => {
      expect(screen.getByRole("button", { name: group })).toBeInTheDocument();
    });

    // Check for updated hint text
    expect(
      screen.getByText(
        /Select a topic group, then choose a category to see questions/i
      )
    ).toBeInTheDocument();
  });

  it("shows Quick Start group expanded by default", () => {
    renderWithProvider(<Home />);

    // Quick Start should be expanded, showing "Getting Started" category
    const gettingStartedButton = screen.getByRole("button", {
      name: "Getting Started",
    });
    expect(gettingStartedButton).toBeInTheDocument();
  });
});
