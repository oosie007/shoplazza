import { Suspense } from "react";
import { AdminNav } from "@/components/admin/AdminNav";

function ItemProtectionLogo({ className }: { className?: string }) {
  return (
    <svg
      className={className}
      width={32}
      height={33}
      viewBox="0 0 32 33"
      fill="none"
      xmlns="http://www.w3.org/2000/svg"
      aria-hidden
    >
      <path
        d="M14.3041 22L9.58773 17.3684L11.0616 15.9211L14.3041 19.1053L22.5579 11L23.8843 12.4474L14.3041 22Z"
        fill="#1A1A1A"
      />
      <path
        d="M16.0784 33H15.7925C15.3493 32.8625 12.1325 31.7625 8.75853 28.0225L8.61556 27.7475L10.2311 26.51L10.374 26.785C12.8617 29.5487 15.2063 30.7863 16.0927 31.0613C17.9941 30.2362 27.2298 25.3962 28.9739 8.415C22.3832 8.14 17.8369 4.27625 16.0927 2.475C14.3342 4.27625 9.80218 8.14 3.21144 8.415C3.65464 13.2413 4.82696 17.5312 6.58544 21.12C6.87138 21.67 7.31457 22.3575 7.6148 23.0588C8.058 23.7463 8.34393 24.1588 8.34393 24.2963L6.72841 25.5338C6.58545 25.3962 6.14225 24.7087 5.69906 24.0212C5.25586 23.3337 4.96993 22.6462 4.6697 22.0825C2.76825 17.9438 1.45296 13.1038 1.00977 7.59C1.00977 7.315 1.15273 7.04 1.2957 6.9025C1.43866 6.765 1.73889 6.49 2.02483 6.6275H2.16779C10.0738 6.6275 15.049 0.55 15.049 0.4125C15.335 0.1375 15.6352 0 16.0784 0C16.3643 0 16.6646 0.1375 16.9505 0.4125C16.9505 0.4125 21.9257 6.6275 29.8317 6.6275H29.9747C30.2606 6.6275 30.5609 6.765 30.7038 6.9025C30.8468 7.04 30.9898 7.315 30.9898 7.59C29.2313 28.2975 16.9362 32.8625 16.35 33H16.0641H16.0784Z"
        fill="#1A1A1A"
      />
    </svg>
  );
}

export default function AdminLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <div className="flex min-h-screen bg-zinc-50">
      <aside className="w-56 shrink-0 border-r border-zinc-200 bg-white">
        <div className="flex h-14 items-center gap-2 border-b border-zinc-200 px-4">
          <ItemProtectionLogo className="h-8 w-8 shrink-0" />
          <span className="font-semibold text-zinc-900">Item Protection</span>
        </div>
        <Suspense fallback={<div className="p-2 text-sm text-zinc-500">Loading…</div>}>
          <AdminNav />
        </Suspense>
      </aside>
      <main className="flex-1 overflow-auto p-6">{children}</main>
    </div>
  );
}
