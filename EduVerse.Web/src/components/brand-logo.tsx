import { EDUVERSE_LOGO_SRC } from "@/lib/brand";
import { cn } from "@/lib/utils";

type BrandLogoProps = {
  className?: string;
  imageClassName?: string;
  showText?: boolean;
  subtitle?: string;
};

export function BrandLogo({
  className,
  imageClassName,
  showText = false,
  subtitle
}: BrandLogoProps) {
  return (
    <div className={cn("flex items-center gap-3", className)}>
      <img
        src={EDUVERSE_LOGO_SRC}
        alt="EduVerse"
        className={cn("h-12 w-auto object-contain", imageClassName)}
      />
      {showText && (
        <div>
          <p className="text-lg font-bold text-ink">EduVerse</p>
          {subtitle && <p className="text-xs text-muted">{subtitle}</p>}
        </div>
      )}
    </div>
  );
}
