import * as React from "react"
import { cva, type VariantProps } from "class-variance-authority"
import { cn } from "@/lib/utils"

const badgeVariants = cva(
  "inline-flex items-center rounded-full px-2 py-0.5 text-[11px] font-mono tracking-tight transition-[background-color,color] focus:outline-none",
  {
    variants: {
      variant: {
        default:
          "border border-zinc-700 bg-zinc-800/80 text-zinc-200",
        secondary:
          "border border-zinc-800 bg-zinc-900 text-zinc-400",
        destructive:
          "border border-rose-900/40 bg-rose-950/30 text-rose-300",
        outline:
          "border border-zinc-800 text-zinc-400",
        accent:
          "border border-blue-500/30 bg-blue-500/10 text-blue-300",
        emerald:
          "border border-emerald-500/30 bg-emerald-500/10 text-emerald-400",
        cyan:
          "border border-cyan-500/30 bg-cyan-500/10 text-cyan-300",
      },
    },
    defaultVariants: {
      variant: "default",
    },
  }
)

export interface BadgeProps
  extends React.HTMLAttributes<HTMLDivElement>,
    VariantProps<typeof badgeVariants> {}

function Badge({ className, variant, ...props }: BadgeProps) {
  return (
    <div className={cn(badgeVariants({ variant }), className)} {...props} />
  )
}

export { Badge, badgeVariants }
