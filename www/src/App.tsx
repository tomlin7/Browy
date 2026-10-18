import React, { useState, useEffect } from "react"
import {
  Download,
  Shield,
  Layers,
  Sparkles,
  Bot,
  Zap,
  Columns4,
  ExternalLink,
  Laptop,
  Check,
  Volume2,
  Moon,
  Search,
  Terminal,
  Activity,
  Cpu,
  Lock,
  ArrowRight,
  Maximize2,
  FileCode,
  Globe,
} from "lucide-react"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"

function GithubIcon({ className = "h-4 w-4" }: { className?: string }) {
  return (
    <svg
      className={className}
      viewBox="0 0 24 24"
      fill="currentColor"
      xmlns="http://www.w3.org/2000/svg"
    >
      <path
        fillRule="evenodd"
        clipRule="evenodd"
        d="M12 2C6.477 2 2 6.484 2 12.017c0 4.425 2.865 8.18 6.839 9.504.5.092.682-.217.682-.483 0-.237-.008-.868-.013-1.703-2.782.605-3.369-1.343-3.369-1.343-.454-1.158-1.11-1.466-1.11-1.466-.908-.62.069-.608.069-.608 1.003.07 1.53 1.032 1.53 1.032.892 1.53 2.341 1.088 2.91.832.092-.647.35-1.088.636-1.338-2.22-.253-4.555-1.113-4.555-4.951 0-1.093.39-1.988 1.029-2.688-.103-.253-.446-1.272.098-2.65 0 0 .84-.27 2.75 1.026A9.564 9.564 0 0112 6.844c.85.004 1.705.115 2.504.337 1.909-1.296 2.747-1.027 2.747-1.027.546 1.379.202 2.398.1 2.651.64.7 1.028 1.595 1.028 2.688 0 3.848-2.339 4.695-4.566 4.943.359.309.678.92.678 1.855 0 1.338-.012 2.419-.012 2.747 0 .268.18.58.688.482A10.019 10.019 0 0022 12.017C22 6.484 17.522 2 12 2z"
      />
    </svg>
  )
}

export default function App() {
  const [activeSpace, setActiveSpace] = useState<"code" | "review" | "research" | "sound">("code")
  const [activeTabTitle, setActiveTabTitle] = useState("tomlin7/Browy · Engine Architecture")
  const [activeDirective, setActiveDirective] = useState("TRANSLATE_SELECTION")
  const [starCount, setStarCount] = useState("★ 1.2k")
  const [copiedRelease, setCopiedRelease] = useState(false)
  const [openFaq, setOpenFaq] = useState<number | null>(0)

  useEffect(() => {
    fetch("https://api.github.com/repos/tomlin7/Browy")
      .then((res) => (res.ok ? res.json() : null))
      .then((data) => {
        if (data && typeof data.stargazers_count === "number") {
          setStarCount(`★ ${data.stargazers_count.toLocaleString()}`)
        }
      })
      .catch(() => {})
  }, [])

  const spaces = {
    code: { label: "DEV.01", name: "Core Engine", badge: "active" },
    review: { label: "PR.35", name: "Code Review", badge: "clean" },
    research: { label: "DOC.AI", name: "LLM Directives", badge: "parsed" },
    sound: { label: "LFI.04", name: "Stream Audio", badge: "playing" },
  }

  const directivesLog = [
    { op: "NAVIGATE", target: "github.com/tomlin7/Browy", latency: "14ms", status: "OK" },
    { op: "PARSE_DOM", target: "article.markdown-body", latency: "38ms", status: "OK" },
    { op: "AI_INSPECT", target: "heuristic_tokenizer_local", latency: "42ms", status: "OFFLINE" },
    { op: "STRIP_TRACKERS", target: "utm_*, gclid, fbclid (4 removed)", latency: "0.2ms", status: "SANITIZED" },
  ]

  const faqs = [
    {
      q: "Is Browy completely free and open source?",
      a: "Yes, 100%. Licensed under MIT. The entire codebase, WPF Direct3D compositing pipelines, and 203 automated test suites are public on GitHub.",
    },
    {
      q: "How does the Autonomous AI Copilot work without compromising privacy?",
      a: "Browy defaults to an offline heuristic synthesis engine that inspects DOM hierarchies locally on your machine with zero cloud roundtrips. When you configure BYOK (OpenAI, Anthropic Claude, or local Ollama), API requests originate straight from your machine using credentials encrypted in Windows DPAPI.",
    },
    {
      q: "What makes it significantly lighter on memory than Google Chrome or Arc?",
      a: "Chrome and Arc spawn independent 300MB+ helper sub-processes for every utility frame. Browy uses the shared Microsoft Edge WebView2 evergreen runtime and enforces automatic tab sleep on idle workspaces, cutting resident memory by over 40%.",
    },
    {
      q: "What are the exact system requirements?",
      a: "Windows 11 or Windows 10 (64-bit). Runs on Microsoft Edge WebView2 (present on 99% of Windows installations) and .NET 9. We also publish a standalone Portable single-file release with zero prerequisites.",
    },
  ]

  const handleCopyInstall = () => {
    navigator.clipboard?.writeText("git clone https://github.com/tomlin7/Browy.git")
    setCopiedRelease(true)
    setTimeout(() => setCopiedRelease(false), 2000)
  }

  return (
    <div className="min-h-[100dvh] bg-[#0c0d0e] text-zinc-100 bg-tech-grid font-sans selection:bg-zinc-800 selection:text-white">
      {/* Top Utility Status Bar */}
      <div className="border-b border-zinc-800/80 bg-zinc-950/70 text-[11px] font-mono text-zinc-400 backdrop-blur-md">
        <div className="max-w-[1400px] mx-auto px-4 sm:px-8 py-1.5 flex items-center justify-between">
          <div className="flex items-center gap-3">
            <span className="inline-block h-1.5 w-1.5 rounded-full bg-emerald-500"></span>
            <span className="text-zinc-300">SYSTEM.OK</span>
            <span className="hidden sm:inline text-zinc-600">/</span>
            <span className="hidden sm:inline text-zinc-400">BROWY v0.3.0 x64</span>
            <span className="hidden md:inline text-zinc-600">/</span>
            <span className="hidden md:inline text-zinc-500">203 UNIT TESTS PASSING</span>
          </div>
          <div className="flex items-center gap-4">
            <span className="hidden sm:inline text-zinc-500 font-mono">MIT LICENSE</span>
            <a
              href="https://github.com/tomlin7/Browy"
              target="_blank"
              rel="noopener noreferrer"
              className="text-zinc-300 hover:text-white transition-colors flex items-center gap-1.5"
            >
              <GithubIcon className="h-3.5 w-3.5" />
              <span>GITHUB</span>
              <span className="text-zinc-500 font-mono">{starCount}</span>
            </a>
          </div>
        </div>
      </div>

      {/* Main Header */}
      <header className="sticky top-0 z-40 border-b border-zinc-800/70 bg-[#0c0d0e]/85 backdrop-blur-md">
        <div className="max-w-[1400px] mx-auto px-4 sm:px-8 h-14 flex items-center justify-between">
          <div className="flex items-center gap-6">
            <a href="#" className="flex items-center gap-2.5">
              <div className="h-6 w-6 rounded bg-zinc-100 flex items-center justify-center text-zinc-950 font-mono font-bold text-xs">
                B
              </div>
              <span className="font-semibold text-sm tracking-tight text-white">Browy</span>
            </a>

            <nav className="hidden lg:flex items-center gap-5 text-xs font-mono text-zinc-400">
              <a href="#spaces" className="hover:text-zinc-200 transition-colors">SPACES</a>
              <a href="#directives" className="hover:text-zinc-200 transition-colors">AUTONOMOUS.AI</a>
              <a href="#specs" className="hover:text-zinc-200 transition-colors">BENCHMARKS</a>
              <a href="#faq" className="hover:text-zinc-200 transition-colors">FAQ</a>
            </nav>
          </div>

          <div className="flex items-center gap-2.5">
            <a href="#download">
              <Button variant="default" size="sm" className="font-mono text-xs gap-1.5 font-semibold">
                <Download className="h-3.5 w-3.5" />
                <span>DOWNLOAD x64</span>
              </Button>
            </a>
          </div>
        </div>
      </header>

      {/* Hero Section: Industrial Asymmetric Layout */}
      <section className="relative border-b border-zinc-800/80 pt-16 pb-20 overflow-hidden">
        <div className="max-w-[1400px] mx-auto px-4 sm:px-8">
          <div className="grid grid-cols-1 lg:grid-cols-12 gap-12 lg:gap-8 items-start">
            {/* Left 5 Cols: Sharp Technical Copy */}
            <div className="lg:col-span-5 flex flex-col justify-center pt-2">
              <div className="inline-flex items-center gap-2 mb-6">
                <span className="px-2 py-0.5 rounded border border-zinc-700 bg-zinc-800/60 font-mono text-[11px] text-zinc-300">
                  RELEASE v0.3.0
                </span>
                <span className="text-zinc-500 font-mono text-xs">WINDOWS 11 NATIVE</span>
              </div>

              <h1 className="text-3xl sm:text-5xl font-medium tracking-tight text-white leading-[1.08] mb-5">
                The AI-Native browser built for focus, not bloat.
              </h1>

              <p className="text-sm sm:text-base text-zinc-400 leading-relaxed mb-8 max-w-[48ch]">
                Tab overload and slow Chromium runtimes are productivity taxes. Browy provides Arc-style spaces,
                offline autonomous AI copilot directives, 4-way split views, and zero telemetry—packaged in a lightweight .NET 9 WPF engine.
              </p>

              <div className="flex flex-col sm:flex-row items-stretch sm:items-center gap-3 mb-8">
                <a href="#download" className="flex-1 sm:flex-none">
                  <Button variant="default" size="lg" className="w-full sm:w-auto font-mono text-xs gap-2">
                    <Laptop className="h-4 w-4" />
                    <span>GET BROWY FOR WINDOWS</span>
                  </Button>
                </a>
                <Button
                  variant="outline"
                  size="lg"
                  onClick={handleCopyInstall}
                  className="font-mono text-xs gap-2 border-zinc-800"
                >
                  {copiedRelease ? <Check className="h-3.5 w-3.5 text-emerald-400" /> : <Terminal className="h-3.5 w-3.5 text-zinc-400" />}
                  <span>{copiedRelease ? "CLONE COMMAND COPIED" : "git clone repo"}</span>
                </Button>
              </div>

              {/* Technical Spec Matrix */}
              <div className="grid grid-cols-3 border-t border-zinc-800/80 pt-6 gap-4 font-mono">
                <div>
                  <div className="text-[10px] text-zinc-500 uppercase">Engine</div>
                  <div className="text-sm font-medium text-zinc-200 mt-0.5">WPF + WebView2</div>
                </div>
                <div>
                  <div className="text-[10px] text-zinc-500 uppercase">Cold Start</div>
                  <div className="text-sm font-medium text-emerald-400 mt-0.5">240ms</div>
                </div>
                <div>
                  <div className="text-[10px] text-zinc-500 uppercase">Telemetry</div>
                  <div className="text-sm font-medium text-zinc-200 mt-0.5">0.0% (Zero)</div>
                </div>
              </div>
            </div>

            {/* Right 7 Cols: Authentic Windows 11 Chrome UI Workspace Preview */}
            <div className="lg:col-span-7">
              <div className="rounded-lg border border-zinc-800 bg-[#121316] shadow-[0_20px_40px_-15px_rgba(0,0,0,0.5)] overflow-hidden">
                {/* Browser Titlebar */}
                <div className="h-10 border-b border-zinc-800/80 bg-zinc-950/80 px-3 flex items-center justify-between">
                  <div className="flex items-center gap-1.5">
                    <span className="h-2.5 w-2.5 rounded-full bg-zinc-700"></span>
                    <span className="h-2.5 w-2.5 rounded-full bg-zinc-700"></span>
                    <span className="h-2.5 w-2.5 rounded-full bg-zinc-700"></span>
                  </div>

                  <div className="flex items-center gap-2 max-w-sm w-full mx-4">
                    <div className="h-6 flex-1 rounded bg-zinc-900 border border-zinc-800/80 px-2.5 flex items-center gap-2 text-[11px] font-mono text-zinc-400 truncate">
                      <Lock className="h-3 w-3 text-zinc-500 shrink-0" />
                      <span className="text-zinc-200 truncate">github.com/tomlin7/Browy</span>
                      <span className="ml-auto text-[9px] text-emerald-400 border border-emerald-900/50 bg-emerald-950/40 px-1 rounded">SHIELDED</span>
                    </div>
                  </div>

                  <div className="flex items-center gap-2 text-zinc-500">
                    <Volume2 className="h-3.5 w-3.5 text-zinc-400" />
                    <Sparkles className="h-3.5 w-3.5 text-zinc-400" />
                  </div>
                </div>

                {/* Body: Left Space Rail + Center Document View + Right AI Directive Stream */}
                <div className="grid grid-cols-1 md:grid-cols-[140px_1fr_200px] h-[380px] text-xs">
                  {/* Space / Tab Column */}
                  <div className="hidden md:flex flex-col border-r border-zinc-800/80 bg-zinc-950/40 p-2.5">
                    <div className="text-[10px] font-mono text-zinc-500 uppercase tracking-wider mb-2">Spaces</div>
                    <div className="space-y-1 mb-4">
                      {(Object.keys(spaces) as Array<keyof typeof spaces>).map((key) => {
                        const s = spaces[key]
                        const isActive = activeSpace === key
                        return (
                          <button
                            key={key}
                            onClick={() => setActiveSpace(key)}
                            className={`w-full text-left px-2 py-1.5 rounded font-mono text-[11px] transition-[background-color,color] flex items-center justify-between ${
                              isActive
                                ? "bg-zinc-800 text-zinc-100 font-semibold"
                                : "text-zinc-400 hover:bg-zinc-900 hover:text-zinc-300"
                            }`}
                          >
                            <span>{s.label}</span>
                            <span className="text-[9px] text-zinc-500">{s.badge}</span>
                          </button>
                        )
                      })}
                    </div>

                    <div className="text-[10px] font-mono text-zinc-500 uppercase tracking-wider mb-2 pt-2 border-t border-zinc-800/60">Tabs (4)</div>
                    <div className="space-y-1 font-mono text-[11px]">
                      <div
                        onClick={() => setActiveTabTitle("tomlin7/Browy · Engine Architecture")}
                        className="px-2 py-1.5 rounded bg-zinc-900/90 text-zinc-200 border border-zinc-800/60 truncate cursor-pointer"
                      >
                        Browy.sln
                      </div>
                      <div
                        onClick={() => setActiveTabTitle("Edge WebView2 Evergreen Docs")}
                        className="px-2 py-1.5 rounded text-zinc-500 hover:text-zinc-300 hover:bg-zinc-900/40 truncate cursor-pointer"
                      >
                        WebView2.md
                      </div>
                      <div
                        onClick={() => setActiveTabTitle("Audio Engine · Ambient Synthesizer")}
                        className="px-2 py-1.5 rounded text-zinc-500 hover:text-zinc-300 hover:bg-zinc-900/40 flex items-center justify-between cursor-pointer"
                      >
                        <span className="truncate">Ambient</span>
                        <span className="text-[9px] text-emerald-400">AUD</span>
                      </div>
                    </div>
                  </div>

                  {/* Active Document Canvas */}
                  <div className="p-4 bg-[#0e0f11] overflow-y-auto font-mono text-xs">
                    <div className="border border-zinc-800 rounded p-3 bg-zinc-950/60 mb-3">
                      <div className="flex items-center justify-between text-[11px] text-zinc-400 mb-2">
                        <span>REPOSITORY: tomlin7/Browy</span>
                        <span className="text-emerald-400">BUILD: PASSING</span>
                      </div>
                      <div className="text-zinc-100 font-sans text-sm font-semibold mb-1">
                        {activeTabTitle}
                      </div>
                      <p className="font-sans text-xs text-zinc-400 leading-relaxed">
                        Hardware-accelerated Direct3D swapchain composition via Microsoft Edge WebView2, supporting dynamic 4-way split layouts and autonomous agent DOM interception.
                      </p>
                    </div>

                    <div className="grid grid-cols-2 gap-2 text-[11px]">
                      <div className="p-2 border border-zinc-800 rounded bg-zinc-950/40">
                        <div className="text-zinc-500">MEMORY PROFILE</div>
                        <div className="text-zinc-200 font-semibold mt-0.5">640 MB (20 tabs)</div>
                      </div>
                      <div className="p-2 border border-zinc-800 rounded bg-zinc-950/40">
                        <div className="text-zinc-500">TAB ISOLATION</div>
                        <div className="text-emerald-400 font-semibold mt-0.5">ACTIVE SLEEP</div>
                      </div>
                    </div>
                  </div>

                  {/* AI Autonomous Directive Stream */}
                  <div className="hidden md:flex flex-col border-l border-zinc-800/80 bg-zinc-950/60 p-3">
                    <div className="flex items-center justify-between text-[10px] font-mono text-zinc-400 uppercase pb-2 border-b border-zinc-800">
                      <span>DOM DIRECTIVES</span>
                      <span className="text-emerald-400 font-bold">ONLINE</span>
                    </div>

                    <div className="flex-1 py-2 space-y-2 font-mono text-[10px] overflow-y-auto">
                      {directivesLog.map((log, i) => (
                        <div key={i} className="p-1.5 rounded border border-zinc-850 bg-zinc-900/40">
                          <div className="flex items-center justify-between text-zinc-300">
                            <span className="font-semibold text-zinc-200">{log.op}</span>
                            <span className="text-zinc-500">{log.latency}</span>
                          </div>
                          <div className="text-zinc-400 truncate mt-0.5">{log.target}</div>
                          <div className="text-[9px] text-emerald-400/90 mt-1">{log.status}</div>
                        </div>
                      ))}
                    </div>

                    <div className="pt-2 border-t border-zinc-800">
                      <div className="text-[10px] font-mono text-zinc-500 mb-1">DISPATCH ACTION:</div>
                      <div className="flex gap-1">
                        {["SCROLL", "CLICK", "EXTRACT"].map((btn) => (
                          <button
                            key={btn}
                            onClick={() => setActiveDirective(btn)}
                            className="flex-1 py-1 rounded border border-zinc-800 bg-zinc-900 text-[9px] font-mono text-zinc-300 hover:bg-zinc-800 transition-colors"
                          >
                            {btn}
                          </button>
                        ))}
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Industrial Spec & Feature Grid */}
      <section id="spaces" className="py-20 border-b border-zinc-800/80">
        <div className="max-w-[1400px] mx-auto px-4 sm:px-8">
          <div className="mb-12">
            <div className="text-xs font-mono text-zinc-500 uppercase tracking-widest mb-1">01 / ARCHITECTURE</div>
            <h2 className="text-2xl sm:text-3xl font-medium tracking-tight text-white">
              Built from first principles for power users.
            </h2>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
            <div className="border border-zinc-800/80 bg-zinc-900/30 rounded p-5 flex flex-col justify-between">
              <div>
                <div className="h-7 w-7 rounded bg-zinc-800 border border-zinc-700/60 flex items-center justify-center text-zinc-300 mb-4">
                  <Layers className="h-4 w-4" />
                </div>
                <h3 className="text-sm font-semibold text-white mb-1.5">Contextual Spaces</h3>
                <p className="text-xs text-zinc-400 leading-relaxed">
                  Isolate work contexts completely: Dev, Review, Research, and Media. Pinned tabs, cookies, and window history remain cleanly partitioned.
                </p>
              </div>
              <div className="mt-6 pt-3 border-t border-zinc-800/60 font-mono text-[11px] text-zinc-500">
                MODULE: SpacesService.cs
              </div>
            </div>

            <div className="border border-zinc-800/80 bg-zinc-900/30 rounded p-5 flex flex-col justify-between">
              <div>
                <div className="h-7 w-7 rounded bg-zinc-800 border border-zinc-700/60 flex items-center justify-center text-zinc-300 mb-4">
                  <Bot className="h-4 w-4" />
                </div>
                <h3 className="text-sm font-semibold text-white mb-1.5">Autonomous AI Directives</h3>
                <p className="text-xs text-zinc-400 leading-relaxed">
                  Autonomous DOM directive protocol (TRANSLATE, CLICK, FILL, ZOOM, READER_MODE) with viewport vision inspection and zero mandatory telemetry.
                </p>
              </div>
              <div className="mt-6 pt-3 border-t border-zinc-800/60 font-mono text-[11px] text-zinc-500">
                MODULE: AiDirectivesCoordinator.cs
              </div>
            </div>

            <div className="border border-zinc-800/80 bg-zinc-900/30 rounded p-5 flex flex-col justify-between">
              <div>
                <div className="h-7 w-7 rounded bg-zinc-800 border border-zinc-700/60 flex items-center justify-center text-zinc-300 mb-4">
                  <Columns4 className="h-4 w-4" />
                </div>
                <h3 className="text-sm font-semibold text-white mb-1.5">4-Way Grid Splitting</h3>
                <p className="text-xs text-zinc-400 leading-relaxed">
                  Side-by-side vertical, horizontal, or 4-quadrant layout grids inside a single window. Code and live documentation synchronized with no OS window clutter.
                </p>
              </div>
              <div className="mt-6 pt-3 border-t border-zinc-800/60 font-mono text-[11px] text-zinc-500">
                MODULE: SplitViewController.cs
              </div>
            </div>

            <div className="border border-zinc-800/80 bg-zinc-900/30 rounded p-5 flex flex-col justify-between">
              <div>
                <div className="h-7 w-7 rounded bg-zinc-800 border border-zinc-700/60 flex items-center justify-center text-zinc-300 mb-4">
                  <Shield className="h-4 w-4" />
                </div>
                <h3 className="text-sm font-semibold text-white mb-1.5">Shields &amp; Tracker Stripper</h3>
                <p className="text-xs text-zinc-400 leading-relaxed">
                  Cleans UTM query parameters, fbclid, and telemetry tokens from URLs before requests ever resolve. Built-in InPrivate cache shredding on workspace close.
                </p>
              </div>
              <div className="mt-6 pt-3 border-t border-zinc-800/60 font-mono text-[11px] text-zinc-500">
                MODULE: ShieldManager.cs
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Hard Benchmarks Section */}
      <section id="specs" className="py-20 border-b border-zinc-800/80">
        <div className="max-w-[1400px] mx-auto px-4 sm:px-8">
          <div className="mb-12">
            <div className="text-xs font-mono text-zinc-500 uppercase tracking-widest mb-1">02 / BENCHMARKS</div>
            <h2 className="text-2xl sm:text-3xl font-medium tracking-tight text-white">
              Light on memory. Heavy on speed.
            </h2>
          </div>

          <div className="grid grid-cols-1 lg:grid-cols-12 gap-8 items-start">
            <div className="lg:col-span-8 border border-zinc-800/80 bg-zinc-900/20 rounded p-6">
              <div className="flex items-center justify-between text-xs font-mono text-zinc-400 mb-6">
                <span>RAM CONSUMPTION (20 TABS OPEN AFTER 15 MINS)</span>
                <span className="text-zinc-500">LOWER IS BETTER</span>
              </div>

              <div className="space-y-5 font-mono text-xs">
                <div>
                  <div className="flex justify-between mb-1.5">
                    <span className="text-emerald-400 font-semibold">Browy (with Idle Suspension)</span>
                    <span className="text-zinc-100 font-bold">640 MB</span>
                  </div>
                  <div className="h-2 rounded-full bg-zinc-800 overflow-hidden">
                    <div className="h-full bg-emerald-500 w-[29.7%]" />
                  </div>
                </div>

                <div>
                  <div className="flex justify-between mb-1.5 text-zinc-400">
                    <span>Arc Browser for Windows</span>
                    <span>1,840 MB</span>
                  </div>
                  <div className="h-2 rounded-full bg-zinc-800 overflow-hidden">
                    <div className="h-full bg-zinc-600 w-[85%]" />
                  </div>
                </div>

                <div>
                  <div className="flex justify-between mb-1.5 text-zinc-400">
                    <span>Google Chrome</span>
                    <span>2,150 MB</span>
                  </div>
                  <div className="h-2 rounded-full bg-zinc-800 overflow-hidden">
                    <div className="h-full bg-zinc-600 w-[100%]" />
                  </div>
                </div>
              </div>

              <div className="mt-8 pt-4 border-t border-zinc-800/60 flex items-center justify-between text-[11px] font-mono text-zinc-500">
                <span>Measured on Windows 11 23H2 x64 (16GB RAM)</span>
                <span className="text-emerald-400 font-medium">-65% vs Chrome</span>
              </div>
            </div>

            <div className="lg:col-span-4 border border-zinc-800/80 bg-zinc-900/20 rounded p-6">
              <div className="text-xs font-mono text-zinc-500 uppercase mb-2">Startup Latency</div>
              <div className="text-5xl font-mono font-medium text-emerald-400 tracking-tight mb-2">
                240<span className="text-lg text-emerald-300">ms</span>
              </div>
              <p className="text-xs text-zinc-400 leading-relaxed mb-6 font-sans">
                Native Direct3D WPF presentation pipelines avoid heavy Electron / Chromium bootstrap layers.
              </p>
              <div className="space-y-2 border-t border-zinc-800/60 pt-4 font-mono text-[11px] text-zinc-400">
                <div className="flex items-center justify-between">
                  <span>Binary Size</span>
                  <span className="text-zinc-200">~12 MB</span>
                </div>
                <div className="flex items-center justify-between">
                  <span>Trackers Blocked</span>
                  <span className="text-emerald-400">100%</span>
                </div>
                <div className="flex items-center justify-between">
                  <span>Test Coverage</span>
                  <span className="text-zinc-200">203 Tests</span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Minimalist Collapsible FAQ */}
      <section id="faq" className="py-20 border-b border-zinc-800/80">
        <div className="max-w-[1400px] mx-auto px-4 sm:px-8">
          <div className="mb-12">
            <div className="text-xs font-mono text-zinc-500 uppercase tracking-widest mb-1">03 / VERIFICATION</div>
            <h2 className="text-2xl sm:text-3xl font-medium tracking-tight text-white">
              Frequently asked questions.
            </h2>
          </div>

          <div className="divide-y divide-zinc-800/80 border-y border-zinc-800/80">
            {faqs.map((faq, idx) => {
              const isOpen = openFaq === idx
              return (
                <div key={idx} className="py-4">
                  <button
                    onClick={() => setOpenFaq(isOpen ? null : idx)}
                    className="w-full flex items-center justify-between text-left text-sm font-medium text-zinc-200 hover:text-white transition-colors"
                  >
                    <span>{faq.q}</span>
                    <span className="font-mono text-xs text-zinc-500 ml-4">{isOpen ? "—" : "+"}</span>
                  </button>
                  {isOpen && (
                    <div className="mt-3 text-xs text-zinc-400 leading-relaxed max-w-[70ch]">
                      {faq.a}
                    </div>
                  )}
                </div>
              )
            })}
          </div>
        </div>
      </section>

      {/* Download Section: Precise Industrial Cards */}
      <section id="download" className="py-20">
        <div className="max-w-[1400px] mx-auto px-4 sm:px-8">
          <div className="mb-12">
            <div className="text-xs font-mono text-zinc-500 uppercase tracking-widest mb-1">04 / INSTALLATION</div>
            <h2 className="text-2xl sm:text-3xl font-medium tracking-tight text-white">
              Get Browy for Windows 11.
            </h2>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-6 max-w-4xl">
            {/* Standard Release */}
            <div className="border border-zinc-700 bg-zinc-900/40 rounded p-6 flex flex-col justify-between">
              <div>
                <div className="flex items-center justify-between mb-3">
                  <span className="font-mono text-xs font-semibold text-zinc-200">STANDARD RELEASE</span>
                  <Badge variant="emerald">RECOMMENDED</Badge>
                </div>
                <p className="text-xs text-zinc-400 leading-relaxed mb-6 font-sans">
                  Lightweight (~12 MB) distribution utilizing system-shared .NET 9 and evergreen WebView2 runtimes.
                </p>
              </div>

              <div>
                <div className="font-mono text-[11px] text-zinc-500 mb-3">
                  PACKAGE: Browy-v0.3.0-win-x64.zip
                </div>
                <a href="https://github.com/tomlin7/Browy/releases/latest" target="_blank" rel="noopener noreferrer">
                  <Button variant="default" className="w-full font-mono text-xs gap-2">
                    <Download className="h-3.5 w-3.5" />
                    <span>DOWNLOAD STANDARD ZIP</span>
                  </Button>
                </a>
              </div>
            </div>

            {/* Portable Edition */}
            <div className="border border-zinc-800 bg-zinc-900/20 rounded p-6 flex flex-col justify-between">
              <div>
                <div className="flex items-center justify-between mb-3">
                  <span className="font-mono text-xs font-semibold text-zinc-200">PORTABLE SINGLE-FILE</span>
                  <Badge variant="secondary">ZERO INSTALL</Badge>
                </div>
                <p className="text-xs text-zinc-400 leading-relaxed mb-6 font-sans">
                  Self-contained single executable containing all native dependencies. Unzip and execute anywhere.
                </p>
              </div>

              <div>
                <div className="font-mono text-[11px] text-zinc-500 mb-3">
                  PACKAGE: Browy-v0.3.0-win-x64-portable.zip
                </div>
                <a href="https://github.com/tomlin7/Browy/releases/latest" target="_blank" rel="noopener noreferrer">
                  <Button variant="outline" className="w-full font-mono text-xs gap-2 border-zinc-800">
                    <Download className="h-3.5 w-3.5" />
                    <span>DOWNLOAD PORTABLE ZIP</span>
                  </Button>
                </a>
              </div>
            </div>
          </div>

          <div className="mt-8 text-xs font-mono text-zinc-500 flex items-center gap-2">
            <Lock className="h-3.5 w-3.5 text-zinc-400" />
            <span>Built reproducibly via GitHub Actions with SHA256 checksum verification.</span>
          </div>
        </div>
      </section>

      {/* Minimal Industrial Footer */}
      <footer className="border-t border-zinc-800/80 bg-zinc-950 py-8 text-xs font-mono text-zinc-500">
        <div className="max-w-[1400px] mx-auto px-4 sm:px-8 flex flex-col sm:flex-row items-center justify-between gap-4">
          <div className="flex items-center gap-3">
            <span className="font-semibold text-zinc-300">BROWY</span>
            <span>/</span>
            <span>&copy; 2026 BROWY CONTRIBUTORS</span>
          </div>

          <div className="flex items-center gap-6">
            <a href="https://github.com/tomlin7/Browy" target="_blank" rel="noopener noreferrer" className="hover:text-zinc-300 transition-colors">GITHUB</a>
            <a href="https://github.com/tomlin7/Browy/releases" target="_blank" rel="noopener noreferrer" className="hover:text-zinc-300 transition-colors">RELEASES</a>
            <a href="https://github.com/tomlin7/Browy/issues" target="_blank" rel="noopener noreferrer" className="hover:text-zinc-300 transition-colors">ISSUES</a>
            <a href="https://github.com/tomlin7/Browy/blob/main/LICENSE" target="_blank" rel="noopener noreferrer" className="hover:text-zinc-300 transition-colors">LICENSE</a>
          </div>
        </div>
      </footer>
    </div>
  )
}
