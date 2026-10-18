import * as React from "react"
import { cva, type VariantProps } from "class-variance-authority"
import { cn } from "@/lib/utils"

const buttonVariants = cva(
  "inline-flex items-center justify-center whitespace-nowrap rounded-md text-sm font-medium transition-[background-color,border-color,color,transform,opacity] duration-150 ease-out focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-zinc-400 disabled:pointer-events-none disabled:opacity-50 enabled:active:scale-[0.96]",
  {
    variants: {
      variant: {
        default:
          "bg-zinc-100 text-zinc-950 hover:bg-white shadow-[0_1px_2px_rgba(0,0,0,0.2)] border border-transparent",
        destructive:
          "bg-rose-950/40 text-rose-300 border border-rose-800/50 hover:bg-rose-900/40",
        outline:
          "border border-zinc-800 bg-zinc-900/60 text-zinc-300 hover:bg-zinc-800/80 hover:text-zinc-100 hover:border-zinc-700",
        secondary:
          "bg-zinc-800 text-zinc-200 hover:bg-zinc-700/80 border border-zinc-750",
        ghost:
          "text-zinc-400 hover:bg-zinc-800/50 hover:text-zinc-200",
        link:
          "text-zinc-300 underline-offset-4 hover:underline hover:text-white",
        tactile:
          "bg-zinc-900 text-zinc-100 border border-zinc-750 shadow-[0_1px_0_0_rgba(255,255,255,0.06)_inset,0_2px_4px_rgba(0,0,0,0.4)] hover:bg-zinc-850 hover:border-zinc-700",
      },
      size: {
        default: "h-9 px-3.5 py-1.5",
        sm: "h-8 rounded px-2.5 text-xs",
        lg: "h-11 rounded-md px-5 text-sm font-medium",
        icon: "h-9 w-9",
      },
    },
    defaultVariants: {
      variant: "default",
      size: "default",
    },
  }
)

export interface ButtonProps
  extends React.ButtonHTMLAttributes<HTMLButtonElement>,
    VariantProps<typeof buttonVariants> {
  asChild?: boolean
}

const Button = React.forwardRef<HTMLButtonElement, ButtonProps>(
  ({ className, variant, size, ...props }, ref) => {
    return (
      <button
        className={cn(buttonVariants({ variant, size, className }))}
        ref={ref}
        {...props}
      />
    )
  }
)
Button.displayName = "Button"

export { Button, buttonVariants }
