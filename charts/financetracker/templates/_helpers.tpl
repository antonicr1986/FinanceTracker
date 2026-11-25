{{/* Etiquetas comunes que Helm y las herramientas esperan en cada recurso. */}}
{{- define "financetracker.labels" -}}
app.kubernetes.io/name: {{ .Chart.Name }}
app.kubernetes.io/instance: {{ .Release.Name }}
app.kubernetes.io/version: {{ .Values.api.image.tag | quote }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
helm.sh/chart: {{ .Chart.Name }}-{{ .Chart.Version }}
{{- end }}

{{/*
securityContext de los contenedores de la API: sin escalar privilegios, sin
capacidades de Linux y con el disco de solo lectura (Trivy KSV-0014/KSV-0118).
*/}}
{{- define "financetracker.containerSecurityContext" -}}
allowPrivilegeEscalation: false
readOnlyRootFilesystem: true
capabilities:
  drop:
    - ALL
{{- end }}
