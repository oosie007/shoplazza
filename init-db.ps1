# Initialize database and start dev server
$env:DATABASE_URL="file:D:/Claude/Johan_Shoplazza/shoplazza/dev.db"

Write-Host "🔧 Generating Prisma client..." -ForegroundColor Cyan
npx prisma generate

Write-Host "📦 Pushing database schema..." -ForegroundColor Cyan
npx prisma db push

Write-Host "🚀 Starting Next.js dev server..." -ForegroundColor Green
npx next dev
