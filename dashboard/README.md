# TaskFlow - Modern Project Management Application

A comprehensive Angular 18+ project management application built with strict TypeScript, signals, standalone components, and full WCAG 2.1 AA accessibility compliance.

## 🚀 Features

### Core Functionality
- **Authentication**: JWT-based authentication with login/register
- **Project Management**: CRUD operations for projects with color coding
- **Task Management**: Full task lifecycle with drag-and-drop Kanban boards
- **Dashboard**: Overview with statistics and recent activities
- **Theme Support**: Dark/light mode with persistence
- **PWA Ready**: Offline support and installable as a native app

### Technical Highlights
- ✅ Angular 18+ with standalone components
- ✅ Strict TypeScript (no `any`, strict null checks)
- ✅ Signals-based reactive state management
- ✅ `inject()` function for dependency injection
- ✅ No HostListener/HostBinding decorators
- ✅ NgOptimizedImage for performance
- ✅ WCAG 2.1 AA accessibility compliance
- ✅ Tailwind CSS for styling
- ✅ CDK Drag & Drop for task boards

## 📁 Project Structure

```
src/app/
├── core/                          # Singleton services and core logic
│   ├── guards/
│   │   └── auth.guard.ts         # Route protection
│   ├── interceptors/
│   │   └── auth.interceptor.ts   # JWT token injection
│   ├── models/
│   │   ├── user.model.ts         # User and auth types
│   │   ├── project.model.ts      # Project types
│   │   ├── task.model.ts         # Task types
│   │   └── team.model.ts         # Team types
│   └── services/
│       ├── auth.service.ts       # Authentication logic
│       ├── project.service.ts    # Project state management
│       ├── task.service.ts       # Task state management
│       └── theme.service.ts      # Theme persistence
├── features/                      # Feature modules
│   ├── auth/
│   │   ├── login/
│   │   │   └── login.component.ts
│   │   ├── register/
│   │   │   └── register.component.ts
│   │   └── auth.routes.ts
│   ├── dashboard/
│   │   └── dashboard.component.ts
│   ├── projects/
│   │   ├── project-list/
│   │   │   └── project-list.component.ts
│   │   ├── project-form/
│   │   │   └── project-form.component.ts
│   │   ├── project-detail/
│   │   │   └── project-detail.component.ts
│   │   └── projects.routes.ts
│   └── tasks/
│       ├── task-board/
│       │   └── task-board.component.ts
│       ├── task-form/
│       │   └── task-form.component.ts
│       └── tasks.routes.ts
├── app.component.ts               # Root component
├── app.routes.ts                  # Application routes
└── main.ts                        # Bootstrap

```

## 🎨 Architecture Patterns

### 1. Signals-Based State Management

All services use Angular signals for reactive state:

```typescript
export class TaskService {
  private readonly tasksSignal = signal<Task[]>([]);
  readonly tasks = this.tasksSignal.asReadonly();
  
  readonly tasksByStatus = computed(() => {
    const allTasks = this.tasksSignal();
    return {
      todo: allTasks.filter(t => t.status === 'todo'),
      // ...
    };
  });
}
```

### 2. Dependency Injection with `inject()`

Modern function-based DI instead of constructor injection:

```typescript
export class LoginComponent {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  
  readonly isAuthenticated = this.authService.isAuthenticated;
}
```

### 3. Standalone Components

All components are standalone with explicit imports:

```typescript
@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  template: `...`
})
export class LoginComponent { }
```

### 4. Functional Guards and Interceptors

Using functional approach instead of classes:

```typescript
export const authGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  return authService.isAuthenticated() || router.createUrlTree(['/auth/login']);
};

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const token = inject(AuthService).token();
  return next(token ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` }}) : req);
};
```

## ♿ Accessibility Features

### WCAG 2.1 AA Compliance

1. **Semantic HTML**: Proper use of `<nav>`, `<main>`, `<section>`, `<article>`
2. **ARIA Labels**: All interactive elements have descriptive labels
3. **Keyboard Navigation**: Full keyboard support with visible focus indicators
4. **Form Validation**: Error messages with `aria-describedby` and `aria-invalid`
5. **Loading States**: `aria-live` regions for dynamic content
6. **Color Contrast**: Minimum 4.5:1 contrast ratio for text
7. **Focus Management**: Logical tab order and focus traps where needed
8. **Screen Reader Support**: Hidden text for icon-only buttons

### Examples

```html
<!-- Labeled interactive elements -->
<button
  [attr.aria-label]="'Edit ' + project.name"
  type="button"
>
  <svg aria-hidden="true">...</svg>
</button>

<!-- Form error handling -->
<input
  id="email"
  [attr.aria-invalid]="emailControl.invalid && emailControl.touched"
  [attr.aria-describedby]="emailControl.invalid ? 'email-error' : null"
/>
<p id="email-error" role="alert">Please enter a valid email</p>

<!-- Loading states -->
<div role="status" aria-live="polite">
  <svg class="animate-spin">...</svg>
  <span class="sr-only">Loading tasks...</span>
</div>
```

## 🔐 Authentication Flow

1. User submits credentials via `LoginComponent`
2. `AuthService.login()` sends request to backend
3. On success, JWT token and user stored in signals + localStorage
4. `authInterceptor` automatically adds token to subsequent requests
5. `authGuard` protects routes from unauthorized access
6. Token expiry triggers automatic logout and redirect

## 📊 State Management Flow

```
Component → Service Method → HTTP Request → Update Signal → UI Updates
                    ↓
              Computed Signals automatically recalculate
                    ↓
              Dependent components re-render
```

### Example: Moving a Task

```typescript
// Component
onDrop(event: CdkDragDrop<Task[]>) {
  // Optimistic update
  transferArrayItem(prev, current, ...);
  
  // Sync with backend
  this.taskService.moveTask(taskId, newStatus, newOrder).subscribe({
    error: () => { /* Revert on failure */ }
  });
}

// Service
moveTask(id: string, status: TaskStatus, order: number) {
  return this.http.patch(`/tasks/${id}`, { status, order }).pipe(
    tap(updated => {
      this.tasksSignal.update(tasks => 
        tasks.map(t => t.id === id ? updated : t)
      );
    })
  );
}
```

## 🎯 Backend Integration

The application expects a REST API with the following endpoints:

### Authentication
- `POST /api/auth/login` - Login with email/password
- `POST /api/auth/register` - Create new account
- `GET /api/auth/me` - Get current user

### Projects
- `GET /api/projects` - List all projects
- `POST /api/projects` - Create project
- `PATCH /api/projects/:id` - Update project
- `DELETE /api/projects/:id` - Delete project

### Tasks
- `GET /api/tasks?projectId=:id` - List tasks (optionally filtered)
- `POST /api/tasks` - Create task
- `PATCH /api/tasks/:id` - Update task
- `DELETE /api/tasks/:id` - Delete task

### Request/Response Examples

```typescript
// Login Request
POST /api/auth/login
{
  "email": "user@example.com",
  "password": "password123"
}

// Login Response
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "user": {
    "id": "user-123",
    "email": "user@example.com",
    "name": "John Doe",
    "role": "member"
  }
}

// Create Task Request
POST /api/tasks
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
{
  "title": "Implement login",
  "description": "Add JWT authentication",
  "status": "todo",
  "priority": "high",
  "projectId": "proj-123",
  "tags": ["auth", "security"]
}
```

## 🚀 Getting Started

### Prerequisites
- Node.js 18+
- Angular CLI 18+

### Installation

```bash
# Clone repository
git clone <repository-url>
cd taskflow

# Install dependencies
npm install

# Start development server
ng serve

# Open browser
http://localhost:4200
```

### Build for Production

```bash
# Production build
ng build --configuration production

# Build with PWA support
ng build --configuration production --service-worker
```

## 🧪 Testing

```bash
# Run unit tests
ng test

# Run e2e tests
ng e2e

# Generate coverage report
ng test --code-coverage
```

## 📱 PWA Configuration

The application is PWA-ready with:
- Service worker for offline caching
- Web manifest for installation
- Optimized assets and lazy loading

To enable PWA features:

```bash
ng add @angular/pwa
```

## 🎨 Customization

### Theme Colors

Edit `tailwind.config.js`:

```javascript
theme: {
  extend: {
    colors: {
      primary: {
        500: '#your-color',
        // ...
      }
    }
  }
}
```

### API Endpoint

Update `API_URL` in service files:

```typescript
const API_URL = 'https://your-backend.com/api';
```

## 📈 Performance Optimizations

1. **Lazy Loading**: All feature modules are lazy-loaded
2. **NgOptimizedImage**: Automatic image optimization
3. **Signals**: Fine-grained reactivity with minimal re-renders
4. **OnPush**: All components use OnPush change detection (via signals)
5. **Tree Shaking**: Unused code automatically removed
6. **PWA Caching**: Static assets cached for offline use

## 🔒 Security Best Practices

1. **JWT Storage**: Tokens in localStorage (consider httpOnly cookies for production)
2. **XSS Prevention**: Angular's built-in sanitization
3. **CSRF Protection**: Implement CSRF tokens in production
4. **Input Validation**: Client and server-side validation
5. **HTTPS Only**: Force HTTPS in production

## 📝 TypeScript Configuration

Strict mode enabled with:
- `noImplicitAny: true`
- `strictNullChecks: true`
- `strictPropertyInitialization: true`
- `noImplicitReturns: true`
- `noFallthroughCasesInSwitch: true`

## 🤝 Contributing

1. Follow Angular style guide
2. Write meaningful commit messages
3. Add tests for new features
4. Ensure accessibility compliance
5. Update documentation

## 📄 License

MIT License - see LICENSE file for details

## 🙏 Acknowledgments

- Angular Team for the amazing framework
- Tailwind CSS for utility-first styling
- Community contributors

---

Built with ❤️ using Angular 18+, TypeScript, and Signals