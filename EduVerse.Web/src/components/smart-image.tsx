"use client";

import { useEffect, useState, type ImgHTMLAttributes } from "react";
import type { Course, CourseAdminDetails } from "@/lib/types";
import { getCourseFallbackImage } from "@/lib/image-fallbacks";

type SmartImageProps = Omit<ImgHTMLAttributes<HTMLImageElement>, "src"> & {
  src?: string;
  fallbackSrc: string;
};

export function SmartImage({ src, fallbackSrc, onError, ...props }: SmartImageProps) {
  const [currentSrc, setCurrentSrc] = useState(src?.trim() || fallbackSrc);

  useEffect(() => {
    setCurrentSrc(src?.trim() || fallbackSrc);
  }, [src, fallbackSrc]);

  return (
    <img
      {...props}
      src={currentSrc}
      onError={(event) => {
        onError?.(event);
        if (currentSrc !== fallbackSrc) {
          setCurrentSrc(fallbackSrc);
        }
      }}
    />
  );
}

export function CourseImage({
  course,
  ...props
}: Omit<SmartImageProps, "src" | "fallbackSrc"> & {
  course: Pick<Course, "name" | "title" | "tags" | "category" | "categories" | "imageUrl">
    | Pick<CourseAdminDetails, "name" | "title" | "category" | "imageUrl">;
}) {
  return (
    <SmartImage
      {...props}
      src={course.imageUrl}
      fallbackSrc={getCourseFallbackImage(course)}
    />
  );
}
