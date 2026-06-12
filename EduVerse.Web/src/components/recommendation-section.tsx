"use client";

import { useEffect, useState } from "react";
import { recommendationService } from "@/lib/api";
import type { Course } from "@/lib/types";
import { CourseCard, EmptyState, LoadingState } from "./ui";

type RecommendationType = "forMe" | "similar" | "trending";

type RecommendationSectionProps = {
  title: string;
  description?: string;
  type: RecommendationType;
  courseId?: string;
  compact?: boolean;
  className?: string;
  showHeading?: boolean;
};

async function loadRecommendations(type: RecommendationType, courseId?: string): Promise<Course[]> {
  if (type === "forMe") {
    return recommendationService.getPersonalizedRecommendations();
  }

  if (type === "similar" && courseId) {
    return recommendationService.getSimilarCourses(courseId);
  }

  if (type === "trending") {
    return recommendationService.getTrendingCourses();
  }

  return [];
}

export function RecommendationSection({
  title,
  description,
  type,
  courseId,
  compact = false,
  className = "mt-8",
  showHeading = true
}: RecommendationSectionProps) {
  const [courses, setCourses] = useState<Course[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    let cancelled = false;

    async function fetchRecommendations() {
      setLoading(true);
      setError("");

      try {
        const data = await loadRecommendations(type, courseId);
        if (!cancelled) {
          setCourses(Array.isArray(data) ? data.filter((course) => Boolean(course?.id)) : []);
        }
      } catch {
        if (!cancelled) {
          setCourses([]);
          setError("Could not load recommendations from the API.");
        }
      } finally {
        if (!cancelled) {
          setLoading(false);
        }
      }
    }

    if (type === "similar" && !courseId) {
      setCourses([]);
      setError("");
      setLoading(false);
      return () => {
        cancelled = true;
      };
    }

    fetchRecommendations();

    return () => {
      cancelled = true;
    };
  }, [type, courseId]);

  return (
    <section className={className}>
      {showHeading && (
        <div>
          <h2 className="text-xl font-bold text-ink">{title}</h2>
          {description && <p className="mt-2 text-sm text-muted">{description}</p>}
        </div>
      )}

      {loading ? (
        <div className="mt-5">
          <LoadingState label={`Loading ${title.toLowerCase()}`} />
        </div>
      ) : error ? (
        <div className="mt-5">
          <EmptyState title="Recommendations unavailable" description={error} />
        </div>
      ) : courses.length === 0 ? (
        <div className="mt-5">
          <EmptyState title="No recommendations yet" description="Check back later as more courses and activity are added to EduVerse." />
        </div>
      ) : (
        <div className="mt-5 grid gap-6 md:grid-cols-2 xl:grid-cols-3">
          {courses.map((course) => (
            <CourseCard key={course.id} course={course} compact={compact} />
          ))}
        </div>
      )}
    </section>
  );
}
