import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { ChatInterface } from "@/components/chat-interface";
import { ChatProvider, useChatContext } from "@/components/chat-context";
import { askQuestionStream } from "@/lib/api";

jest.mock("@/lib/api", () => ({
  ...jest.requireActual("@/lib/api"),
  askQuestionStream: jest.fn(),
}));

const mockedAskQuestionStream = askQuestionStream as jest.MockedFunction<
  typeof askQuestionStream
>;

function ExternalResetButton() {
  const { resetChat } = useChatContext();
  return <button onClick={resetChat}>External reset</button>;
}

describe("ChatInterface reset", () => {
  beforeEach(() => {
    mockedAskQuestionStream.mockImplementation(async (_question, callbacks) => {
      callbacks.onSources([]);
      callbacks.onDelta("Streamed answer");
      callbacks.onDone();
    });
  });

  afterEach(() => {
    jest.resetAllMocks();
  });

  const renderChat = () =>
    render(
      <ChatProvider>
        <ExternalResetButton />
        <ChatInterface />
      </ChatProvider>
    );

  const askQuestion = async (user: ReturnType<typeof userEvent.setup>) => {
    await user.type(
      screen.getByPlaceholderText(/Ask a question about your documents/i),
      "What is the fee?{Enter}"
    );
    await screen.findByText("Streamed answer");
  };

  it("clears the conversation when New chat is clicked", async () => {
    const user = userEvent.setup();
    renderChat();
    await askQuestion(user);

    await user.click(screen.getByRole("button", { name: /New chat/i }));

    expect(screen.queryByText("Streamed answer")).not.toBeInTheDocument();
    expect(
      screen.getByRole("heading", { name: /Ask anything about your funds/i })
    ).toBeInTheDocument();
  });

  it("clears the conversation when reset is triggered from the context", async () => {
    const user = userEvent.setup();
    renderChat();
    await askQuestion(user);

    await user.click(screen.getByRole("button", { name: "External reset" }));

    expect(screen.queryByText("Streamed answer")).not.toBeInTheDocument();
  });

  it("supports a new conversation after a reset", async () => {
    const user = userEvent.setup();
    renderChat();
    await askQuestion(user);
    await user.click(screen.getByRole("button", { name: /New chat/i }));

    await askQuestion(user);

    expect(screen.getAllByText("Streamed answer")).toHaveLength(1);
  });
});
