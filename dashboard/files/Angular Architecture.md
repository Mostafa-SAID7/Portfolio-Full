src/
├── app/
│   ├── core/                     # singletons: auth.service, http.interceptor
│   ├── shared/                   # ui + pipes + directives
│   ├── features/
│   │   ├── auth/                 # login, register, guards
│   │   ├── dashboard/            # main layout
│   │   ├── projects/             # list + detail views
│   │   └── tasks/                # task board, CRUD dialogs
│   ├── state/                    # signals, computed(), effect()
│   ├── app.routes.ts             # standalone routing
│   └── app.component.ts          # shell + theme toggle
├── assets/
└── environments/