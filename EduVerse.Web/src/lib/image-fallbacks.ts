import type { Course, CourseAdminDetails } from "./types";

export const SEED_IMAGES = {
  angular: "/images/seed/course-angular.webp",
  react: "/images/seed/course-react.webp",
  dotnet: "/images/seed/course-dotnet.webp",
  sql: "/images/seed/course-sql.webp",
  java: "/images/seed/course-java.webp",
  mobile: "/images/seed/course-mobile.webp",
  dataAi: "/images/seed/course-data-ai.webp",
  cybersecurity: "/images/seed/course-cybersecurity.webp",
  cloudDevops: "/images/seed/course-cloud-devops.webp",
  design: "/images/seed/course-design.webp",
  businessMarketing: "/images/seed/course-business-marketing.webp",
  communication: "/images/seed/course-communication.webp",
  wellness: "/images/seed/course-wellness.webp",
  photography: "/images/seed/course-photography.webp",
  testing: "/images/seed/course-testing.webp",
  profile: "/images/seed/profile-avatar.webp",
  organization: "/images/seed/organization.webp",
  certificate: "/images/seed/certificate.webp",
  learningHero: "/images/seed/learning-hero.webp"
} as const;

const SEMANTIC_IMAGES: Record<string, string> = {
  "seed:course-angular": SEED_IMAGES.angular,
  "seed:course-react": SEED_IMAGES.react,
  "seed:course-dotnet": SEED_IMAGES.dotnet,
  "seed:course-sql": SEED_IMAGES.sql,
  "seed:course-java": SEED_IMAGES.java,
  "seed:course-mobile": SEED_IMAGES.mobile,
  "seed:course-data-ai": SEED_IMAGES.dataAi,
  "seed:course-cybersecurity": SEED_IMAGES.cybersecurity,
  "seed:course-cloud-devops": SEED_IMAGES.cloudDevops,
  "seed:course-design": SEED_IMAGES.design,
  "seed:course-business-marketing": SEED_IMAGES.businessMarketing,
  "seed:course-communication": SEED_IMAGES.communication,
  "seed:course-wellness": SEED_IMAGES.wellness,
  "seed:course-photography": SEED_IMAGES.photography,
  "seed:course-testing": SEED_IMAGES.testing,
  "seed:profile-avatar": SEED_IMAGES.profile,
  "seed:organization": SEED_IMAGES.organization,
  "seed:certificate": SEED_IMAGES.certificate
};

type CourseImageContext = Pick<Course, "name" | "title" | "tags" | "category" | "categories">
  | Pick<CourseAdminDetails, "name" | "title" | "category">;

export function resolveSemanticImage(value?: string) {
  if (!value) return undefined;
  return SEMANTIC_IMAGES[value.trim().toLowerCase()];
}

export function isMissingImageReference(value?: string) {
  if (!value?.trim()) return true;
  const normalized = value.trim().toLowerCase();
  return normalized.includes("placeholder")
    || normalized.includes("placehold.co")
    || normalized.includes("via.placeholder.com")
    || /^[0-9a-f-]{36}-thumbnail$/i.test(normalized);
}

export function getCourseFallbackImage(course: CourseImageContext) {
  const categories = "categories" in course
    ? course.categories?.map((category) => category.name).join(" ")
    : "";
  const context = `${course.title ?? ""} ${course.name ?? ""} ${"tags" in course ? course.tags ?? "" : ""} ${course.category ?? ""} ${categories}`.toLowerCase();

  if (hasAny(context, "angular")) return SEED_IMAGES.angular;
  if (hasAny(context, "react native", "react-native", "flutter", "mobile")) return SEED_IMAGES.mobile;
  if (hasAny(context, "react", "javascript", "frontend", "typescript")) return SEED_IMAGES.react;
  if (hasAny(context, "sql server", "database", "sql")) return SEED_IMAGES.sql;
  if (hasAny(context, "java", "spring")) return SEED_IMAGES.java;
  if (hasAny(context, "asp.net", "aspnet", ".net", "dotnet", "c#", "csharp", "entity framework")) return SEED_IMAGES.dotnet;
  if (hasAny(context, "python", "machine learning", "data science", "analytics", "pandas")) return SEED_IMAGES.dataAi;
  if (hasAny(context, "cyber", "security", "ethical hacking", "networking")) return SEED_IMAGES.cybersecurity;
  if (hasAny(context, "devops", "docker", "kubernetes", "cloud", "aws", "ci/cd", "cicd")) return SEED_IMAGES.cloudDevops;
  if (hasAny(context, "testing", "selenium", "quality assurance", "qa")) return SEED_IMAGES.testing;
  if (hasAny(context, "photography", "photoshop", "photo editing")) return SEED_IMAGES.photography;
  if (hasAny(context, "design", "figma", "ui", "ux", "graphic")) return SEED_IMAGES.design;
  if (hasAny(context, "health", "fitness", "nutrition", "wellness", "personal development")) return SEED_IMAGES.wellness;
  if (hasAny(context, "language", "english", "german", "french", "spanish", "leadership", "public speaking", "communication")) return SEED_IMAGES.communication;
  if (hasAny(context, "marketing", "sales", "business", "entrepreneurship", "finance", "accounting", "project management")) return SEED_IMAGES.businessMarketing;

  return SEED_IMAGES.dotnet;
}

function hasAny(value: string, ...terms: string[]) {
  return terms.some((term) => value.includes(term));
}
