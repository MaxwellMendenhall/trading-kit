/// <reference types="vite/client" />
interface ImportMetaEnv {
    readonly VITE_BACKEND: string,
}

interface ImportMeta {
    readonly env: ImportMetaEnv
}