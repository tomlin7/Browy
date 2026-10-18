import React, { useState, useEffect } from "react"
import {
  Download,
  Star,
  Shield,
  Layers,
  Sparkles,
  Bot,
  Zap,
  Columns4,
  ExternalLink,
  Laptop,
  CheckCircle2,
  Terminal,
  Volume2,
  Moon,
  Search,
  BookOpen,
} from "lucide-react"
import { Button } from "@/components/ui/button"

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
import { Card, CardHeader, CardTitle, CardDescription, CardContent } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"
import {
  Accordion,
  AccordionItem,
  AccordionTrigger,
  AccordionContent,
} from "@/components/ui/accordion"

export default function App() {
  const [activeSpace, setActiveSpace] = useState<"work" | "dev" | "research" | "media">("work")
  const [activeTabTitle, setActiveTabTitle] = useState("tomlin7/Browy: Modern Arc/Zen Browser")
  const [copilotPrompt, setCopilotPrompt] = useState("translate to German")
  const [chatMessages, setChatMessages] = useState<Array<{ role: "user" | "agent"; text: string; action?: string }>>([
    {
      role: "user",
      text: "Analyze this page and find the latest autonomous browser action features.",
    },
    {
      role: "agent",
      text: "Examined tomlin7/Browy architecture snapshot: Autonomous DOM actions protocol (TRANSLATE, CLICK, FILL, ZOOM, READER_MODE), real-time viewport vision analysis, and local heuristic fallback.",
      action: "Autonomous Action: Scrolled to top of page",
    },
  ])

  const [starCount, setStarCount] = useState("★ 1.2k")

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

  const spaceThemes = {
    work: { name: "Work Space", color: "text-blue-400", border: "border-blue-500/40", bg: "bg-blue-500/10" },
    dev: { name: "Developer Space", color: "text-purple-400", border: "border-purple-500/40", bg: "bg-purple-500/10" },
    research: { name: "Research Space", color: "text-emerald-400", border: "border-emerald-500/40", bg: "bg-emerald-500/10" },
    media: { name: "Media Space", color: "text-pink-400", border: "border-pink-500/40", bg: "bg-pink-500/10" },
  }

  const handleChipClick = (prompt: string) => {
    setCopilotPrompt(prompt)
    const userMsg = { role: "user" as const, text: prompt }
    let agentText = "Processing page context live via Browy AI Copilot..."
    let actionDesc = "Executed browser command"

    if (prompt.includes("Summarize")) {
      agentText = "Executive Summary: Browy delivers a clean, tab-isolated experience built on .NET 9 and Edge WebView2 with 40% memory reduction."
      actionDesc = "Autonomous Action: Highlighted core concepts"
    } else if (prompt.includes("Translate")) {
      agentText = "Traduction en Français: Browy est le navigateur le plus moderne pour Windows 11 avec des espaces de travail fluides."
      actionDesc = "Autonomous Action: Translated active page to French"
    } else {
      agentText = "DOM Technical Analysis: TLS 1.3 encrypted origin, 203 automated test suites verified, zero third-party trackers detected."
      actionDesc = "Autonomous Action: Inspected DOM tree"
    }

    setChatMessages((prev) => [
      ...prev,
      userMsg,
      { role: "agent", text: agentText, action: actionDesc },
    ])
  }

  return (
    <div className="relative min-h-screen bg-[#07080D] text-slate-100 selection:bg-indigo-500 selection:text-white">
      {/* Ambient background glow orbs */}
      <div className="glow-orb orb-primary bg-indigo-600/30 w-[600px] h-[600px] top-[-150px] left-[20%]" />
      <div className="glow-orb orb-secondary bg-blue-600/25 w-[500px] h-[500px] top-[400px] right-[-100px]" />
      <div className="glow-orb orb-accent bg-purple-600/20 w-[700px] h-[700px] top-[1500px] left-[-200px]" />

      {/* Navigation Bar */}
      <header className="sticky top-0 z-50 border-b border-white/10 bg-[#07080D]/75 backdrop-blur-2xl">
        <div className="mx-auto flex max-w-7xl items-center justify-between px-6 py-3.5">
          <a href="#" className="flex items-center gap-3">
            <div className="flex h-9 w-9 items-center justify-center rounded-xl bg-gradient-to-br from-blue-500 via-indigo-500 to-pink-500 shadow-lg shadow-indigo-500/25">
              <span className="font-extrabold text-white text-base">B</span>
            </div>
            <span className="text-xl font-extrabold tracking-tight text-white">Browy</span>
            <Badge variant="indigo" className="font-mono text-[11px] px-2">v0.3.0</Badge>
          </a>

          <nav className="hidden md:flex items-center gap-7 text-sm font-medium text-slate-400">
            <a href="#features" className="hover:text-white transition-colors">Features</a>
            <a href="#copilot" className="hover:text-white transition-colors">AI Copilot</a>
            <a href="#spaces" className="hover:text-white transition-colors">Spaces &amp; Tabs</a>
            <a href="#shields" className="hover:text-white transition-colors">Shields</a>
            <a href="#benchmarks" className="hover:text-white transition-colors">Benchmarks</a>
            <a href="#faq" className="hover:text-white transition-colors">FAQ</a>
          </nav>

          <div className="flex items-center gap-3">
            <a
              href="https://github.com/tomlin7/Browy"
              target="_blank"
              rel="noopener noreferrer"
              className="hidden sm:inline-flex"
            >
              <Button variant="outline" size="sm" className="gap-2 border-white/15 bg-white/5 hover:bg-white/10">
                <GithubIcon className="h-4 w-4" />
                <span>Star</span>
                <span className="rounded bg-white/10 px-1.5 py-0.5 text-[11px] text-slate-300 font-mono">
                  {starCount}
                </span>
              </Button>
            </a>

            <a href="#download">
              <Button variant="gradient" size="sm" className="gap-2 font-semibold">
                <Download className="h-4 w-4" />
                <span>Download</span>
              </Button>
            </a>
          </div>
        </div>
      </header>

      {/* Hero Section */}
      <section className="relative z-10 pt-16 pb-24 text-center">
        <div className="mx-auto max-w-5xl px-6">
          <div className="mb-6 inline-flex items-center gap-2.5 rounded-full border border-white/10 bg-white/[0.03] px-4 py-1.5 text-xs text-slate-300 backdrop-blur-md">
            <span className="h-2 w-2 rounded-full bg-emerald-400 shadow-[0_0_10px_#34d399]" />
            <span>v0.3.0 Release: Autonomous AI Directives &amp; Search Bangs Hub</span>
            <a href="https://github.com/tomlin7/Browy/releases" target="_blank" className="font-semibold text-blue-400 hover:underline">
              Changelog &rarr;
            </a>
          </div>

          <h1 className="text-4xl sm:text-6xl lg:text-7xl font-extrabold tracking-tight leading-[1.1] mb-6">
            The Fluid, AI-Native Browser <br />
            <span className="bg-gradient-to-r from-blue-400 via-indigo-400 to-pink-400 bg-clip-text text-transparent">
              Built for Windows 11.
            </span>
          </h1>

          <p className="mx-auto max-w-3xl text-lg sm:text-xl text-slate-400 leading-relaxed mb-10">
            Say goodbye to browser bloat and tab clutter. Browy fuses Arc-inspired spaces,
            autonomous AI agent directives, 4-way split views, and native Mica/Acrylic materials
            into a lightweight, hyper-responsive power tool.
          </p>

          <div className="flex flex-col sm:flex-row items-center justify-center gap-4 mb-10">
            <a href="#download">
              <Button variant="gradient" size="lg" className="h-14 px-8 text-base gap-3.5 shadow-2xl shadow-indigo-600/40">
                <Laptop className="h-5 w-5" />
                <div className="text-left">
                  <div className="text-[10px] uppercase font-bold tracking-wider opacity-85">Download for Windows</div>
                  <div className="text-sm font-extrabold">Browy v0.3.0 (x64)</div>
                </div>
              </Button>
            </a>

            <a href="https://github.com/tomlin7/Browy" target="_blank" rel="noopener noreferrer">
              <Button variant="outline" size="lg" className="h-14 px-6 gap-2.5 border-white/15 bg-white/5 hover:bg-white/10">
                <GithubIcon className="h-5 w-5" />
                <span>Explore Source Code</span>
                <ExternalLink className="h-3.5 w-3.5 opacity-60" />
              </Button>
            </a>
          </div>

          {/* Feature highlights pills */}
          <div className="flex flex-wrap justify-center gap-2.5 text-xs text-slate-400 mb-14">
            <span className="rounded-full border border-white/10 bg-white/[0.02] px-3.5 py-1">🛡️ 100% Free &amp; Open Source (MIT)</span>
            <span className="rounded-full border border-white/10 bg-white/[0.02] px-3.5 py-1">⚡ Microsoft Edge WebView2</span>
            <span className="rounded-full border border-white/10 bg-white/[0.02] px-3.5 py-1">🚀 .NET 9.0 Native Desktop</span>
            <span className="rounded-full border border-white/10 bg-white/[0.02] px-3.5 py-1">🔒 Zero Telemetry &amp; Tracker Stripper</span>
          </div>

          {/* Interactive Browser Mockup */}
          <div className="mx-auto max-w-5xl rounded-2xl border border-white/15 bg-[#0D1017] shadow-2xl shadow-black/80 overflow-hidden text-left">
            {/* Titlebar */}
            <div className="flex items-center justify-between border-b border-white/10 bg-[#121520] px-4 py-2.5">
              <div className="flex items-center gap-2">
                <span className="h-3 w-3 rounded-full bg-amber-400" />
                <span className="h-3 w-3 rounded-full bg-emerald-400" />
                <span className="h-3 w-3 rounded-full bg-rose-400" />
              </div>
              <div className="flex items-center gap-2 text-xs text-slate-300">
                <span className={`px-2 py-0.5 rounded font-bold uppercase text-[10px] border ${spaceThemes[activeSpace].bg} ${spaceThemes[activeSpace].color} ${spaceThemes[activeSpace].border}`}>
                  {spaceThemes[activeSpace].name}
                </span>
                <span className="truncate max-w-sm font-medium">{activeTabTitle}</span>
              </div>
              <div className="flex items-center gap-2 text-slate-400 text-xs">
                <Shield className="h-3.5 w-3.5 text-emerald-400" />
                <Volume2 className="h-3.5 w-3.5 text-blue-400" />
                <Sparkles className="h-3.5 w-3.5 text-indigo-400" />
              </div>
            </div>

            {/* Browser Body */}
            <div className="grid grid-cols-1 md:grid-cols-[200px_1fr_300px] h-[480px]">
              {/* Left Sidebar */}
              <div className="hidden md:flex flex-col border-r border-white/10 bg-[#0F121C] p-3 gap-3">
                {/* Spaces switcher */}
                <div className="flex gap-1.5 pb-2 border-b border-white/10">
                  {(["work", "dev", "research", "media"] as const).map((s) => (
                    <button
                      key={s}
                      onClick={() => setActiveSpace(s)}
                      className={`h-7 w-7 rounded-md font-bold text-xs uppercase transition-colors ${
                        activeSpace === s ? "bg-blue-600 text-white" : "bg-white/5 text-slate-400 hover:bg-white/10"
                      }`}
                      title={spaceThemes[s].name}
                    >
                      {s[0]}
                    </button>
                  ))}
                </div>

                {/* Open tabs */}
                <div className="text-[10px] font-bold uppercase tracking-wider text-slate-400 px-1">Tabs (4)</div>
                <div className="flex flex-col gap-1 text-xs">
                  <div
                    onClick={() => setActiveTabTitle("tomlin7/Browy: Modern Arc/Zen Browser")}
                    className="flex items-center gap-2 rounded-lg bg-white/10 p-2 font-medium text-white cursor-pointer"
                  >
                    <span>🐙</span>
                    <span className="truncate flex-1">tomlin7/Browy</span>
                  </div>
                  <div
                    onClick={() => setActiveTabTitle("Microsoft Edge WebView2 Docs")}
                    className="flex items-center gap-2 rounded-lg p-2 text-slate-400 hover:bg-white/5 cursor-pointer"
                  >
                    <span>📘</span>
                    <span className="truncate flex-1">WebView2 Docs</span>
                  </div>
                  <div
                    onClick={() => setActiveTabTitle("Lo-Fi Synthwave Stream")}
                    className="flex items-center gap-2 rounded-lg p-2 text-slate-400 hover:bg-white/5 cursor-pointer"
                  >
                    <span>📺</span>
                    <span className="truncate flex-1">Lo-Fi Stream</span>
                    <Badge variant="emerald" className="text-[9px] px-1 py-0">AUDIO</Badge>
                  </div>
                  <div
                    onClick={() => setActiveTabTitle("ArXiv: Autonomous Browser Agents")}
                    className="flex items-center gap-2 rounded-lg p-2 text-slate-400 hover:bg-white/5 cursor-pointer opacity-60"
                  >
                    <span>💤</span>
                    <span className="truncate flex-1">ArXiv: Agents</span>
                    <Badge variant="secondary" className="text-[9px] px-1 py-0">ASLEEP</Badge>
                  </div>
                </div>
              </div>

              {/* Main Content Area */}
              <div className="flex flex-col bg-[#080A10]">
                {/* Omnibox Navbar */}
                <div className="flex items-center gap-2 border-b border-white/10 bg-[#111420] px-3 py-2">
                  <div className="flex-1 flex items-center gap-2 rounded-lg border border-white/10 bg-[#161B29] px-3 py-1.5 text-xs text-white">
                    <span className="text-slate-400">🔒</span>
                    <span className="font-mono text-slate-200 truncate flex-1">https://github.com/tomlin7/Browy</span>
                    <span className="rounded-full bg-emerald-500/20 px-2 py-0.5 text-[10px] font-semibold text-emerald-400 border border-emerald-500/30">
                      5 Trackers Blocked
                    </span>
                  </div>
                  <Button variant="ghost" size="icon" className="h-7 w-7 text-slate-400">◫</Button>
                  <Button variant="ghost" size="icon" className="h-7 w-7 text-slate-400">📖</Button>
                </div>

                {/* Mock Viewport */}
                <div className="flex-1 p-6 overflow-y-auto">
                  <div className="rounded-xl border border-white/10 bg-white/[0.02] p-5">
                    <div className="flex items-center gap-2 text-xs text-slate-400 mb-2">
                      <span className="rounded bg-white/10 px-2 py-0.5">Public</span>
                      <span>tomlin7 / <strong>Browy</strong></span>
                    </div>
                    <h2 className="text-lg font-bold text-white mb-2">Modern Arc/Zen-inspired Windows 11 browser built with .NET 9 WPF &amp; Edge WebView2.</h2>
                    <div className="grid grid-cols-3 gap-3 my-4">
                      <div className="rounded-lg border border-white/10 bg-[#10131E] p-3 text-center">
                        <div className="text-xl font-extrabold text-blue-400">203</div>
                        <div className="text-[11px] text-slate-400">Unit Tests Passing</div>
                      </div>
                      <div className="rounded-lg border border-white/10 bg-[#10131E] p-3 text-center">
                        <div className="text-xl font-extrabold text-emerald-400">60+ FPS</div>
                        <div className="text-[11px] text-slate-400">Fluent Direct3D</div>
                      </div>
                      <div className="rounded-lg border border-white/10 bg-[#10131E] p-3 text-center">
                        <div className="text-xl font-extrabold text-purple-400">-40%</div>
                        <div className="text-[11px] text-slate-400">RAM with Tab Sleep</div>
                      </div>
                    </div>
                    <div className="rounded-md border border-white/10 bg-black/50 p-2.5 font-mono text-xs text-slate-300">
                      git clone https://github.com/tomlin7/Browy.git
                    </div>
                  </div>
                </div>
              </div>

              {/* Right AI Copilot Pane */}
              <div className="hidden md:flex flex-col border-l border-white/10 bg-[#10131E]">
                <div className="flex items-center justify-between border-b border-white/10 p-3">
                  <div className="flex items-center gap-1.5 text-xs font-bold text-white">
                    <Sparkles className="h-4 w-4 text-indigo-400" />
                    <span>AI Copilot</span>
                  </div>
                  <Badge variant="indigo" className="text-[10px]">Deep Research</Badge>
                </div>

                <div className="flex-1 p-3 space-y-3 overflow-y-auto text-xs">
                  {chatMessages.map((msg, idx) => (
                    <div key={idx} className={`flex ${msg.role === "user" ? "justify-end" : "justify-start"}`}>
                      <div className={`rounded-xl p-2.5 max-w-[90%] ${
                        msg.role === "user"
                          ? "bg-blue-600 text-white"
                          : "bg-[#181C2B] border border-white/10 text-slate-200"
                      }`}>
                        <p>{msg.text}</p>
                        {msg.action && (
                          <div className="mt-2 inline-flex items-center gap-1 rounded bg-emerald-500/20 px-2 py-0.5 text-[10px] font-semibold text-emerald-300 border border-emerald-500/30">
                            <CheckCircle2 className="h-3 w-3" />
                            <span>{msg.action}</span>
                          </div>
                        )}
                      </div>
                    </div>
                  ))}
                </div>

                <div className="p-3 border-t border-white/10">
                  <div className="flex gap-1.5 mb-2 overflow-x-auto pb-1">
                    {["Summarize", "Translate", "Code Analysis"].map((chip) => (
                      <button
                        key={chip}
                        onClick={() => handleChipClick(chip)}
                        className="rounded-full border border-white/10 bg-white/5 px-2.5 py-0.5 text-[10px] text-slate-300 hover:bg-white/15 whitespace-nowrap transition-colors"
                      >
                        {chip}
                      </button>
                    ))}
                  </div>
                  <div className="flex gap-1.5">
                    <input
                      type="text"
                      value={copilotPrompt}
                      onChange={(e) => setCopilotPrompt(e.target.value)}
                      placeholder="Ask copilot or run command..."
                      className="flex-1 rounded-md border border-white/10 bg-[#161A28] px-2.5 py-1.5 text-xs text-white focus:outline-none focus:border-indigo-500"
                    />
                    <Button
                      size="sm"
                      onClick={() => handleChipClick(copilotPrompt)}
                      className="h-8 px-2.5 bg-indigo-600 hover:bg-indigo-500"
                    >
                      &rarr;
                    </Button>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Feature Grid Section */}
      <section id="features" className="relative z-10 py-24 border-t border-white/10 bg-[#0A0D15]/60 backdrop-blur-md">
        <div className="mx-auto max-w-6xl px-6">
          <div className="text-center mb-16">
            <Badge variant="cyan" className="mb-3 uppercase tracking-widest text-[11px] font-bold">Engineered for Windows 11</Badge>
            <h2 className="text-3xl sm:text-5xl font-extrabold tracking-tight mb-4 text-white">Everything modern browsing should be.</h2>
            <p className="mx-auto max-w-2xl text-slate-400 text-base sm:text-lg">
              Browy re-imagines daily web navigation from first principles: distraction-free focus,
              instant speed, and total ownership of your AI &amp; browsing data.
            </p>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
            {/* Card 1 */}
            <Card className="md:col-span-2 border-indigo-500/30 bg-gradient-to-br from-[#12162A] to-[#0D101E]">
              <CardHeader>
                <div className="h-12 w-12 rounded-xl bg-indigo-500/20 text-indigo-400 flex items-center justify-center mb-3 border border-indigo-500/30">
                  <Bot className="h-6 w-6" />
                </div>
                <CardTitle className="text-2xl">Autonomous AI Copilot &amp; Multimodal Vision</CardTitle>
                <CardDescription className="text-base text-slate-300">
                  Equipped with BYOK for OpenAI, Anthropic Claude, and local Ollama. The Copilot doesn't just chat—it can autonomously navigate, scroll, fill forms, translate pages, and analyze live viewport snapshots.
                </CardDescription>
              </CardHeader>
              <CardContent className="space-y-2 text-sm text-slate-300">
                <div className="flex items-center gap-2"><CheckCircle2 className="h-4 w-4 text-emerald-400" /> Multimodal vision screenshot analysis streamed directly to LLM</div>
                <div className="flex items-center gap-2"><CheckCircle2 className="h-4 w-4 text-emerald-400" /> Autonomous DOM directives: CLICK, FILL, ZOOM, TRANSLATE, READER_MODE</div>
                <div className="flex items-center gap-2"><CheckCircle2 className="h-4 w-4 text-emerald-400" /> 4 Specialized Personas: Deep Research, Code Analyst, Explainer, Copilot</div>
                <div className="flex items-center gap-2"><CheckCircle2 className="h-4 w-4 text-emerald-400" /> Complete offline heuristic mode with zero telemetry</div>
              </CardContent>
            </Card>

            {/* Card 2 */}
            <Card>
              <CardHeader>
                <div className="h-12 w-12 rounded-xl bg-purple-500/20 text-purple-400 flex items-center justify-center mb-3 border border-purple-500/30">
                  <Layers className="h-6 w-6" />
                </div>
                <CardTitle>Arc &amp; Zen-Inspired Spaces</CardTitle>
                <CardDescription>
                  Organize your mental state into distinct spaces: Work, Personal, Dev, Research, and Media. Each space maintains its own pinned tabs and isolated context.
                </CardDescription>
              </CardHeader>
              <CardContent className="space-y-2 text-sm text-slate-300">
                <div className="flex items-center gap-2"><CheckCircle2 className="h-4 w-4 text-emerald-400" /> Instant cross-space tab migrations</div>
                <div className="flex items-center gap-2"><CheckCircle2 className="h-4 w-4 text-emerald-400" /> Auto-collapsing vertical sidebar</div>
              </CardContent>
            </Card>

            {/* Card 3 */}
            <Card>
              <CardHeader>
                <div className="h-12 w-12 rounded-xl bg-blue-500/20 text-blue-400 flex items-center justify-center mb-3 border border-blue-500/30">
                  <Columns4 className="h-6 w-6" />
                </div>
                <CardTitle>Up to 4-Way Split Screen</CardTitle>
                <CardDescription>
                  Forget messy window tiling. Multitask side-by-side with 2-way horizontal/vertical or 4-way grid layouts inside a single sleek browser window.
                </CardDescription>
              </CardHeader>
              <CardContent className="space-y-2 text-sm text-slate-300">
                <div className="flex items-center gap-2"><CheckCircle2 className="h-4 w-4 text-emerald-400" /> Code &amp; documentation side-by-side</div>
                <div className="flex items-center gap-2"><CheckCircle2 className="h-4 w-4 text-emerald-400" /> Synchronized scrolling toggles</div>
              </CardContent>
            </Card>

            {/* Card 4 */}
            <Card>
              <CardHeader>
                <div className="h-12 w-12 rounded-xl bg-emerald-500/20 text-emerald-400 flex items-center justify-center mb-3 border border-emerald-500/30">
                  <Shield className="h-6 w-6" />
                </div>
                <CardTitle>Built-in Privacy Shields</CardTitle>
                <CardDescription>
                  Block intrusive ad networks, isolate third-party cookies, and automatically strip tracking parameters (utm_*, fbclid, gclid) from all URLs.
                </CardDescription>
              </CardHeader>
              <CardContent className="space-y-2 text-sm text-slate-300">
                <div className="flex items-center gap-2"><CheckCircle2 className="h-4 w-4 text-emerald-400" /> Aggressive URL UTM tracker cleaner</div>
                <div className="flex items-center gap-2"><CheckCircle2 className="h-4 w-4 text-emerald-400" /> Automated InPrivate cache shredder</div>
              </CardContent>
            </Card>

            {/* Card 5 */}
            <Card>
              <CardHeader>
                <div className="h-12 w-12 rounded-xl bg-amber-500/20 text-amber-400 flex items-center justify-center mb-3 border border-amber-500/30">
                  <Zap className="h-6 w-6" />
                </div>
                <CardTitle>Smart Bangs &amp; Omnibox Hub</CardTitle>
                <CardDescription>
                  Execute instant bang searches directly from the address bar (!gh, !yt, !w, !so) or solve mathematical formulas on the fly.
                </CardDescription>
              </CardHeader>
              <CardContent className="space-y-2 text-sm text-slate-300">
                <div className="flex items-center gap-2"><CheckCircle2 className="h-4 w-4 text-emerald-400" /> Hub at browy://searchengines</div>
                <div className="flex items-center gap-2"><CheckCircle2 className="h-4 w-4 text-emerald-400" /> Inline math calculator</div>
              </CardContent>
            </Card>
          </div>
        </div>
      </section>

      {/* Benchmarks Section */}
      <section id="benchmarks" className="relative z-10 py-24">
        <div className="mx-auto max-w-6xl px-6">
          <div className="text-center mb-16">
            <Badge variant="emerald" className="mb-3 uppercase tracking-widest text-[11px] font-bold">Memory &amp; Efficiency</Badge>
            <h2 className="text-3xl sm:text-5xl font-extrabold tracking-tight mb-4 text-white">Light on RAM. Heavy on Speed.</h2>
            <p className="mx-auto max-w-2xl text-slate-400 text-base sm:text-lg">
              Unlike monolithic Chromium browsers that spawn 50 background helper processes, Browy leverages
              WebView2's shared runtime and proactive idle tab suspension to dramatically reduce system resource consumption.
            </p>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-6 items-center">
            <Card className="md:col-span-2 p-6">
              <div className="text-lg font-bold text-white mb-6">RAM Usage (20 Tabs Open after 15 mins)</div>
              <div className="space-y-4">
                <div>
                  <div className="flex justify-between text-xs text-slate-300 mb-1.5">
                    <span className="font-semibold text-emerald-400">Browy (with Idle Suspension)</span>
                    <span className="font-bold">640 MB</span>
                  </div>
                  <div className="h-3 rounded-full bg-white/10 overflow-hidden">
                    <div className="h-full bg-gradient-to-r from-blue-500 to-emerald-400 w-[30%]" />
                  </div>
                </div>

                <div>
                  <div className="flex justify-between text-xs text-slate-400 mb-1.5">
                    <span>Arc Browser for Windows</span>
                    <span>1,840 MB</span>
                  </div>
                  <div className="h-3 rounded-full bg-white/10 overflow-hidden">
                    <div className="h-full bg-slate-600 w-[80%]" />
                  </div>
                </div>

                <div>
                  <div className="flex justify-between text-xs text-slate-400 mb-1.5">
                    <span>Google Chrome</span>
                    <span>2,150 MB</span>
                  </div>
                  <div className="h-3 rounded-full bg-white/10 overflow-hidden">
                    <div className="h-full bg-slate-600 w-[94%]" />
                  </div>
                </div>
              </div>
              <div className="mt-6 text-xs text-slate-400">
                Average RAM measured on Windows 11 23H2 (x64) with 20 mixed documentation &amp; media tabs after 15 minutes of idle suspension.
              </div>
            </Card>

            <Card className="p-6 text-center">
              <div className="text-sm font-semibold text-slate-400 mb-2">Cold Startup Time</div>
              <div className="text-6xl font-extrabold text-emerald-400 tracking-tight my-2">240<span className="text-2xl text-emerald-300">ms</span></div>
              <p className="text-xs text-slate-400 mb-4 leading-relaxed">
                Powered by native .NET 9 desktop runtime and optimized Direct3D composition pipelines.
              </p>
              <div className="flex flex-wrap gap-2 justify-center text-xs">
                <Badge variant="emerald">✓ Fast AOT</Badge>
                <Badge variant="emerald">✓ Direct3D</Badge>
              </div>
            </Card>
          </div>
        </div>
      </section>

      {/* FAQ Section */}
      <section id="faq" className="relative z-10 py-24 border-t border-white/10 bg-[#0A0D15]/60 backdrop-blur-md">
        <div className="mx-auto max-w-4xl px-6">
          <div className="text-center mb-16">
            <Badge variant="indigo" className="mb-3 uppercase tracking-widest text-[11px] font-bold">Frequently Asked Questions</Badge>
            <h2 className="text-3xl sm:text-5xl font-extrabold tracking-tight mb-4 text-white">Everything you need to know.</h2>
          </div>

          <Accordion defaultValue={["faq-1"]} className="space-y-4">
            <AccordionItem value="faq-1">
              <AccordionTrigger>Is Browy completely free and open source?</AccordionTrigger>
              <AccordionContent>
                Yes, 100%! Browy is licensed under the MIT License. The complete source code, 203 automated unit tests, and build definitions are available publicly on GitHub. You can inspect, fork, compile, and contribute without restrictions.
              </AccordionContent>
            </AccordionItem>

            <AccordionItem value="faq-2">
              <AccordionTrigger>How does the AI Copilot work and is my data private?</AccordionTrigger>
              <AccordionContent>
                Browy puts privacy first. By default, Browy uses an offline heuristic synthesis engine that never sends any page data to the cloud. If you configure BYOK (OpenAI, Claude, or Ollama), requests are sent directly to your chosen API provider using your own API keys stored securely on your machine via Windows DPAPI.
              </AccordionContent>
            </AccordionItem>

            <AccordionItem value="faq-3">
              <AccordionTrigger>What are the system requirements to run Browy?</AccordionTrigger>
              <AccordionContent>
                Browy requires a 64-bit Windows 11 or Windows 10 operating system, the Microsoft Edge WebView2 Evergreen Runtime (pre-installed on almost all modern Windows systems), and .NET 9.0. We also provide a Self-Contained Portable Edition that runs out of the box with zero dependencies.
              </AccordionContent>
            </AccordionItem>

            <AccordionItem value="faq-4">
              <AccordionTrigger>How do I install or update Browy?</AccordionTrigger>
              <AccordionContent>
                Download the latest release ZIP from the Download section below or from GitHub Releases. Extract the ZIP anywhere on your system and launch Browy.exe. Browy is completely portable and requires no installer or administrator privileges.
              </AccordionContent>
            </AccordionItem>
          </Accordion>
        </div>
      </section>

      {/* Download Section */}
      <section id="download" className="relative z-10 py-24">
        <div className="mx-auto max-w-5xl px-6">
          <div className="rounded-3xl border border-indigo-500/30 bg-gradient-to-br from-[#12162B] to-[#0A0D18] p-10 sm:p-16 text-center shadow-2xl shadow-indigo-500/10">
            <div className="inline-flex h-14 w-14 items-center justify-center rounded-2xl bg-gradient-to-br from-blue-500 via-indigo-500 to-pink-500 shadow-xl mb-6">
              <span className="font-extrabold text-white text-2xl">B</span>
            </div>
            <h2 className="text-3xl sm:text-5xl font-extrabold tracking-tight text-white mb-4">
              Ready to elevate your browsing?
            </h2>
            <p className="mx-auto max-w-xl text-slate-400 text-base sm:text-lg mb-12">
              Get Browy v0.3.0 for Windows 11 &amp; 10. Fast, lightweight, open-source, and free forever.
            </p>

            <div className="grid grid-cols-1 sm:grid-cols-2 gap-6 max-w-3xl mx-auto mb-8 text-left">
              <Card className="border-blue-500/50 bg-[#0F1322] p-6 flex flex-col justify-between">
                <div>
                  <Badge variant="cyan" className="mb-2">RECOMMENDED</Badge>
                  <h3 className="text-xl font-bold text-white mb-1">Standard Release</h3>
                  <p className="text-xs text-slate-400 mb-6 leading-relaxed">
                    Optimized package using shared .NET 9 and WebView2 runtimes. Ultra-compact download size (~12 MB).
                  </p>
                </div>
                <a href="https://github.com/tomlin7/Browy/releases/latest" target="_blank" rel="noopener noreferrer">
                  <Button variant="default" className="w-full gap-2 font-semibold">
                    <Download className="h-4 w-4" />
                    <span>Download Browy-v0.3.0.zip</span>
                  </Button>
                </a>
              </Card>

              <Card className="border-white/10 bg-[#0F1322] p-6 flex flex-col justify-between">
                <div>
                  <Badge variant="secondary" className="mb-2">PORTABLE</Badge>
                  <h3 className="text-xl font-bold text-white mb-1">Portable Edition</h3>
                  <p className="text-xs text-slate-400 mb-6 leading-relaxed">
                    Self-contained single executable. Zero runtime installation required. Unzip and run anywhere.
                  </p>
                </div>
                <a href="https://github.com/tomlin7/Browy/releases/latest" target="_blank" rel="noopener noreferrer">
                  <Button variant="outline" className="w-full gap-2 font-semibold border-white/15 hover:bg-white/10">
                    <Download className="h-4 w-4" />
                    <span>Download Portable Edition</span>
                  </Button>
                </a>
              </Card>
            </div>

            <div className="text-xs text-slate-400">
              🔒 All releases are built transparently via GitHub Actions with published SHA256 checksums.
            </div>
          </div>
        </div>
      </section>

      {/* Footer */}
      <footer className="border-t border-white/10 bg-[#05060A] py-12 text-xs text-slate-400">
        <div className="mx-auto max-w-6xl px-6 flex flex-col sm:flex-row items-center justify-between gap-6">
          <div className="flex items-center gap-3">
            <span className="font-extrabold text-white text-base">Browy</span>
            <Badge variant="outline" className="text-[10px]">MIT License</Badge>
            <span>&copy; 2026 Browy Contributors.</span>
          </div>

          <div className="flex items-center gap-6">
            <a href="https://github.com/tomlin7/Browy" target="_blank" rel="noopener noreferrer" className="hover:text-white transition-colors">GitHub</a>
            <a href="https://github.com/tomlin7/Browy/releases" target="_blank" rel="noopener noreferrer" className="hover:text-white transition-colors">Releases</a>
            <a href="https://github.com/tomlin7/Browy/issues" target="_blank" rel="noopener noreferrer" className="hover:text-white transition-colors">Issues</a>
            <a href="https://github.com/tomlin7/Browy/blob/main/LICENSE" target="_blank" rel="noopener noreferrer" className="hover:text-white transition-colors">License</a>
          </div>
        </div>
      </footer>
    </div>
  )
}
