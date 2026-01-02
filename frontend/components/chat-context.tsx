"use client";

import { createContext, useContext, useState, ReactNode } from "react";

interface ChatContextType {
  shouldReset: boolean;
  resetChat: () => void;
  clearReset: () => void;
}

const ChatContext = createContext<ChatContextType | undefined>(undefined);

export function ChatProvider({ children }: { children: ReactNode }) {
  const [shouldReset, setShouldReset] = useState(false);

  const resetChat = () => {
    setShouldReset(true);
  };

  const clearReset = () => {
    setShouldReset(false);
  };

  return (
    <ChatContext.Provider value={{ shouldReset, resetChat, clearReset }}>
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
