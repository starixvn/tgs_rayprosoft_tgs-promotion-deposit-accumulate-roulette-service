{{/*
Expand the name of the chart.
*/}}
{{- define "deposit-accumulate-roulette.name" -}}
{{- default .Chart.Name .Values.nameOverride | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Create a default fully qualified app name.
We truncate at 63 chars because some Kubernetes name fields are limited to this (by the DNS naming spec).
If release name contains chart name it will be used as a full name.
*/}}
{{- define "deposit-accumulate-roulette.fullname" -}}
{{- if .Values.fullnameOverride }}
{{- .Values.fullnameOverride | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- $name := default .Chart.Name .Values.nameOverride }}
{{- if contains $name .Release.Name }}
{{- .Release.Name | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- printf "%s-%s" .Release.Name $name | trunc 63 | trimSuffix "-" }}
{{- end }}
{{- end }}
{{- end }}

{{/*
Create chart name and version as used by the chart label.
*/}}
{{- define "deposit-accumulate-roulette.chart" -}}
{{- printf "%s-%s" .Chart.Name .Chart.Version | replace "+" "_" | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Common labels
*/}}
{{- define "deposit-accumulate-roulette.labels" -}}
helm.sh/chart: {{ include "deposit-accumulate-roulette.chart" . }}
{{ include "deposit-accumulate-roulette.selectorLabels" . }}
{{- if .Chart.AppVersion }}
app.kubernetes.io/version: {{ .Chart.AppVersion | quote }}
{{- end }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
{{- end }}

{{/*
Selector labels
*/}}
{{- define "deposit-accumulate-roulette.selectorLabels" -}}
app.kubernetes.io/name: {{ include "deposit-accumulate-roulette.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
tgs.kind: promotion
{{- end }}

{{/*
Create the name of the service account to use
*/}}
{{- define "deposit-accumulate-roulette.serviceAccountName" -}}
{{- if .Values.serviceAccount.create }}
{{- default (include "deposit-accumulate-roulette.fullname" .) .Values.serviceAccount.name }}
{{- else }}
{{- default "default" .Values.serviceAccount.name }}
{{- end }}
{{- end }}
