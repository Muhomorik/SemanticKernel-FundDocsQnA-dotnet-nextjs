"use client";

import { createContext, useContext, useState, ReactNode } from "react";

interface ChatContextType {
  // Incremented on every reset; used as a React key to remount the chat session
  resetKey: number;
  resetChat: () => void;
}

const ChatContext = createContext<ChatContextType | undefined>(undefined);

export function ChatProvider({ children }: { children: ReactNode }) {
  const [resetKey, setResetKey] = useState(0);

  const resetChat = () => {
    setResetKey((key) => key + 1);
  };

  return (
    <ChatContext.Provider value={{ resetKey, resetChat }}>
      {children}
    </ChatContext.Provider>
  );
}

export function useChatContext() {
  const context = useContext(ChatContext);
  if (!context) {
    throw new Error("useChatContext must be used within ChatProvider");
  }
  return context;
}
