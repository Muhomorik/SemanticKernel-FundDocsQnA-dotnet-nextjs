# AboutFund WebView2 Response Interception

Network response interception for the AboutFund browser window — captures fund API responses from WebView2 and forwards them to the application layer orchestrator.

## Data Flow

Runtime sequence from WebView2 network response through to the orchestrator and UI.

```mermaid
sequenceDiagram
    participant WV2 as WebView2
    participant INT as AboutFundResponseInterceptor
    participant ORC as IAboutFundOrchestrator
    participant VM as AboutFundWindowViewModel
    participant UI as XAML Bindings

    WV2->>INT: WebResourceResponseReceived
    INT->>INT: ShouldInterceptResponse(uri)
    alt URL matches fund API pattern
        INT->>INT: ExtractResponsePreviewAsync()
        INT->>INT: Create AboutFundInterceptedRequest
        INT-->>VM: RequestIntercepted event (UI)
        INT->>ORC: NotifyResponseCaptured(request)
        ORC-->>ORC: Log / process response data
    end

    Note over VM,UI: Separate orchestrator streams
    ORC-->>VM: SessionState (IObservable)
    VM-->>UI: Property change notifications
    ORC-->>VM: NavigateToUrl (IObservable)
    VM-->>WV2: BrowserUrl binding
```

## Architecture

The interceptor bridges Presentation → Application directly, keeping the code-behind free of application-layer dependencies:

```mermaid
flowchart TB
    subgraph Presentation["Presentation Layer (Wpf)"]
        subgraph View["AboutFundWindow.xaml"]
            direction LR
            WV["WebView2<br/>(with attached behavior)"]
            IP["Interceptor Panel<br/>- DataGrid (requests)<br/>- Response preview"]
        end

        subgraph CodeBehind["AboutFundWindow.xaml.cs<br/>(UI plumbing only)"]
            CB1["CoreWebView2InitializationCompleted"]
            CB2["interceptor.Initialize(Browser)"]
            CB3["interceptor.Dispose()"]
        end

        INT["AboutFundResponseInterceptor"]
        VM["AboutFundWindowViewModel"]
    end

    subgraph Application["Application Layer"]
        ORC["IAboutFundOrchestrator"]
    end

    WV -->|"AboutFundWebView2Behavior"| WV
    WV -->|"CoreWebView2InitializationCompleted"| CodeBehind
    CodeBehind -->|"Initialize(Browser)"| INT
    INT -->|"RequestIntercepted event"| VM
    INT -->|"NotifyResponseCaptured()"| ORC
    VM -->|"session commands +<br/>observable streams"| ORC
```

## Type Dependencies

Constructor injection dependencies between the AboutFund interception components. Arrows show "depends on" direction — all flow toward the Application layer (DDD-compliant).

```mermaid
classDiagram
    direction LR

    class AboutFundWindow {
        -ILogger _logger
        -AboutFundWindowViewModel _viewModel
        -IAboutFundResponseInterceptor _interceptor
    }

    class AboutFundResponseInterceptor {
        -ILogger _logger
        -IAboutFundOrchestrator _orchestrator
        +Initialize(WebView2)
        +RequestIntercepted : event
    }

    class IAboutFundResponseInterceptor {
        <<interface>>
        +Initialize(WebView2)
        +RequestIntercepted : event
    }

    class AboutFundWindowViewModel {
        -ILogger _logger
        -IAboutFundOrchestrator _orchestrator
        -IScheduler _uiScheduler
    }

    class IAboutFundOrchestrator {
        <<interface>>
        +NotifyResponseCaptured(request)
        +SessionState : IObservable
        +Events : IObservable
        +NavigateToUrl : IObservable
    }

    class AboutFundInterceptedRequest {
        +string Id
        +DateTime Timestamp
        +string Method
        +string Url
        +int StatusCode
        +string ContentType
        +long ContentLength
        +string? ResponsePreview
    }

    AboutFundWindow --> IAboutFundResponseInterceptor
    AboutFundWindow --> AboutFundWindowViewModel
    AboutFundResponseInterceptor ..|> IAboutFundResponseInterceptor
    AboutFundResponseInterceptor --> IAboutFundOrchestrator
    AboutFundResponseInterceptor ..> AboutFundInterceptedRequest : creates
    AboutFundWindowViewModel --> IAboutFundOrchestrator
```

## Files

**Services:**

- `Services/IAboutFundResponseInterceptor.cs` — Interface for response interception

  ```csharp
  public interface IAboutFundResponseInterceptor : IDisposable
  {
      void Initialize(WebView2 webView);
      event EventHandler<AboutFundInterceptedRequest>? RequestIntercepted;
  }
  ```

- `Services/AboutFundResponseInterceptor.cs` — Implementation:
  - Subscribes to `CoreWebView2.WebResourceResponseReceived`
  - Captures network requests matching fund API URL patterns
  - Extracts response content preview (first 2KB for JSON/text)
  - Raises `RequestIntercepted` event for UI consumers
  - Calls `IAboutFundOrchestrator.NotifyResponseCaptured()` directly (DDD: Presentation → Application)

**Models:**

- `Application/Models/AboutFundInterceptedRequest.cs` — Captured request/response data

**Behaviors:**

- `Behaviors/AboutFundWebView2Behavior.cs` — Attached behavior for WebView2 initialization and navigation events

**ViewModels:**

- `ViewModels/AboutFundWindowViewModel.cs` — Window state, commands, orchestrator subscriptions

**Views:**

- `Views/AboutFundWindow.xaml` — 3-column layout (schedule | browser | control panel)
- `Views/AboutFundWindow.xaml.cs` — Code-behind (UI plumbing only: interceptor init + dispose, no application-layer dependencies)

## Testing

### Manual Checklist

- [ ] AboutFund button opens non-modal window
- [ ] WebView2 loads default URL
- [ ] Network requests appear in interceptor panel
- [ ] Status codes are color-coded (green=2xx, orange=4xx, red=5xx)
- [ ] Clicking a request shows response preview
- [ ] NLog output shows `"Response captured:"` trace from orchestrator

### Enable Logging

```xml
<logger name="YieldRaccoon.Wpf.Services.AboutFundResponseInterceptor" minlevel="Trace" writeTo="file" />
<logger name="YieldRaccoon.Wpf.Behaviors.AboutFundWebView2Behavior" minlevel="Debug" writeTo="file" />
<logger name="YieldRaccoon.Infrastructure.Services.AboutFundOrchestrator" minlevel="Trace" writeTo="file" />
```

### Common Issues

| Issue | Solution |
| ----- | -------- |
| No data intercepted | Check URL patterns in `ShouldInterceptResponse()`, enable Trace logging |
| Empty response content | Response stream consumed by another reader — check for `COMException` in logs |
| Events not reaching UI | Verify `RequestIntercepted` subscription, check UI thread marshalling with `ObserveOn` |
