import * as React from "react"
import { ChevronDown } from "lucide-react"
import { cn } from "@/lib/utils"

interface AccordionContextType {
  openItems: string[]
  toggleItem: (value: string) => void
}

const AccordionContext = React.createContext<AccordionContextType | null>(null)

export function Accordion({
  children,
  defaultValue = [],
  className,
}: {
  children: React.ReactNode
  defaultValue?: string[]
  className?: string
}) {
  const [openItems, setOpenItems] = React.useState<string[]>(defaultValue)

  const toggleItem = (val: string) => {
    setOpenItems((prev) =>
      prev.includes(val) ? prev.filter((i) => i !== val) : [...prev, val]
    )
  }

  return (
    <AccordionContext.Provider value={{ openItems, toggleItem }}>
      <div className={cn("space-y-3", className)}>{children}</div>
    </AccordionContext.Provider>
  )
}

export function AccordionItem({
  value,
  children,
  className,
}: {
  value: string
  children: React.ReactNode
  className?: string
}) {
  return (
    <div
      className={cn(
        "rounded-xl border border-border/80 bg-card/60 backdrop-blur-md overflow-hidden transition-colors hover:border-border",
        className
      )}
      data-item-value={value}
    >
      {React.Children.map(children, (child) => {
        if (React.isValidElement(child)) {
          return React.cloneElement(child as React.ReactElement<{ itemValue?: string }>, {
            itemValue: value,
          })
        }
        return child
      })}
    </div>
  )
}

export function AccordionTrigger({
  children,
  itemValue,
  className,
}: {
  children: React.ReactNode
  itemValue?: string
  className?: string
}) {
  const ctx = React.useContext(AccordionContext)
  if (!ctx || !itemValue) return null

  const isOpen = ctx.openItems.includes(itemValue)

  return (
    <button
      type="button"
      onClick={() => ctx.toggleItem(itemValue)}
      className={cn(
        "flex w-full items-center justify-between p-5 text-left font-semibold text-foreground transition-all",
        className
      )}
    >
      <span>{children}</span>
      <ChevronDown
        className={cn(
          "h-5 w-5 text-indigo-400 transition-transform duration-200",
          isOpen && "rotate-180"
        )}
      />
    </button>
  )
}

export function AccordionContent({
  children,
  itemValue,
  className,
}: {
  children: React.ReactNode
  itemValue?: string
  className?: string
}) {
  const ctx = React.useContext(AccordionContext)
  if (!ctx || !itemValue) return null

  const isOpen = ctx.openItems.includes(itemValue)
  if (!isOpen) return null

  return (
    <div
      className={cn(
        "px-5 pb-5 pt-0 text-sm leading-relaxed text-muted-foreground animate-in fade-in-50 duration-200",
        className
      )}
    >
      {children}
    </div>
  )
}
