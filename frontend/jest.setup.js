// Learn more: https://github.com/testing-library/jest-dom
import "@testing-library/jest-dom";

// Mock scrollIntoView since it's not available in jsdom
Element.prototype.scrollIntoView = jest.fn();
