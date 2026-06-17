"use client";

import { Download, ExternalLink } from "lucide-react";
import { downloadFile, openFile } from "@/lib/api";
import { cn } from "@/lib/utils";
import { Button } from "./ui";

type FileActionButtonsProps = {
  url?: string;
  previewLabel?: string;
  downloadLabel?: string;
  className?: string;
  fullWidth?: boolean;
  showPreview?: boolean;
};

export function FileActionButtons({
  url,
  previewLabel = "Open",
  downloadLabel = "Download",
  className,
  fullWidth = false,
  showPreview = true
}: FileActionButtonsProps) {
  if (!url) return null;

  const stretchClassName = fullWidth ? "flex-1" : "";

  return (
    <div className={cn("flex flex-wrap gap-2", fullWidth && "w-full", className)}>
      {showPreview && (
        <Button type="button" className={stretchClassName} onClick={() => openFile(url)}>
          <ExternalLink size={16} />
          {previewLabel}
        </Button>
      )}
      <Button type="button" variant="ghost" className={stretchClassName} onClick={() => downloadFile(url)}>
        <Download size={16} />
        {downloadLabel}
      </Button>
    </div>
  );
}
