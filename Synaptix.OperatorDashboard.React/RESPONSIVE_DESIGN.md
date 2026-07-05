# Responsive Design Guide

## Breakpoints

```
xs  320px  - Small phones
sm  375px  - Standard phones
md  768px  - Tablets (portrait)
lg  1024px - Tablets (landscape) / Small laptops
xl  1280px - Desktops
2xl 1536px - Large desktops
```

## Key Responsive Patterns

### 1. Grid Layouts

**Desktop (4 columns)**
```jsx
<div className="grid grid-cols-4 gap-4">
  {/* 4 cards in a row */}
</div>
```

**Tablet (2 columns)**
```jsx
<div className="grid grid-cols-2 md:grid-cols-4 gap-4">
  {/* 2 cards on md, 4 on desktop */}
</div>
```

**Mobile (1 column)**
```jsx
<div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
  {/* 1 card on mobile, 2 on tablet, 4 on desktop */}
</div>
```

### 2. Sidebar Navigation

**Mobile: Hamburger menu**
```jsx
<MobileNav>
  {/* Nav links */}
</MobileNav>
```

**Desktop: Fixed sidebar**
```jsx
<div className="hidden md:block fixed sidebar-w">
  {/* Sidebar */}
</div>
<div className="md:ml-sidebar-w">
  {/* Main content */}
</div>
```

### 3. Tables on Mobile

**Desktop: Full table**
```jsx
<div className="overflow-x-auto">
  <table className="w-full">
    {/* Full table */}
  </table>
</div>
```

**Mobile: Card view**
```jsx
<div className="md:hidden space-y-4">
  {data.map(item => (
    <div key={item.id} className="border rounded p-4">
      {/* Card representation */}
    </div>
  ))}
</div>
<div className="hidden md:block">
  <table>{/* Table */}</table>
</div>
```

### 4. Touch Targets

Ensure interactive elements are at least 44×44px on mobile:

```jsx
<button className="touch-target">Action</button>
```

### 5. Padding & Spacing

Mobile-first spacing:
```jsx
<div className="p-4 md:p-6 lg:p-8">
  {/* More padding on larger screens */}
</div>
```

### 6. Typography

```jsx
<h1 className="text-2xl md:text-3xl lg:text-4xl font-bold">
  Responsive heading
</h1>
```

## Mobile Checklist

- [ ] All buttons/links ≥ 44×44px
- [ ] No horizontal scroll on any device
- [ ] Text is readable without zoom (≥16px on mobile)
- [ ] Touch targets have adequate spacing
- [ ] Images scale responsively with `max-w-full`
- [ ] Forms are mobile-friendly (full width on mobile)
- [ ] Modals/dialogs work on small screens
- [ ] Navigation is accessible on mobile (hamburger menu)

## Testing

```bash
# Dev tools - Device toolbar
DevTools > Toggle device toolbar (Ctrl+Shift+M)

# Chrome DevTools sizes
- iPhone SE: 375×667
- iPhone 12: 390×844
- iPad: 768×1024
- Desktop: 1920×1080
```

## Dark Mode Integration

Dark mode is automatically applied based on:
1. System preference (`prefers-color-scheme`)
2. User toggle (stored in localStorage)
3. Manual theme selection

All color utilities automatically adapt:
```jsx
<div className="bg-bg-primary text-ink-primary">
  {/* Light mode: off-white bg, dark text */}
  {/* Dark mode: dark bg, light text (automatic) */}
</div>
```

## Performance Considerations

1. **Lazy load images** on mobile:
   ```jsx
   <img src={url} loading="lazy" />
   ```

2. **Avoid large hero images** on mobile
3. **Minimize table columns** on mobile
4. **Stack modals vertically** on small screens
5. **Collapse accordions** to save space

## Examples

All dashboard modules follow these patterns:
- `/users` - Table with mobile card fallback
- `/store` - Grid with responsive columns
- `/config` - Accordion on mobile, tabs on desktop
- `/notifications` - Full-width forms on mobile

See implementation in `src/features/*/pages/`
