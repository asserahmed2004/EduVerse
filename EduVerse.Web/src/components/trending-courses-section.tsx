"use client";

import { RecommendationSection } from "@/components/recommendation-section";
import { LinkButton } from "@/components/ui";

export function TrendingCoursesSection() {
  return (
    <section id="courses" className="mx-auto max-w-7xl px-5 py-12 lg:px-8">
      <div className="flex flex-col justify-between gap-4 md:flex-row md:items-end">
        <div>
          <p className="text-sm font-semibold text-teal-600">Trending now</p>
          <h2 className="mt-2 text-3xl font-bold text-ink">Popular courses on EduVerse</h2>
        </div>
        <LinkButton href="/courses" variant="ghost">View all</LinkButton>
      </div>

      <RecommendationSection
        title="Trending Courses"
        type="trending"
        className="mt-8"
        showHeading={false}
      />
    </section>
  );
}
