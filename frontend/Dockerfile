FROM php:8.3-fpm-alpine

RUN apk add --no-cache nginx supervisor curl-dev \
    && docker-php-ext-install session curl \
    && mkdir -p /run/nginx /var/log/supervisor

COPY nginx.conf /etc/nginx/http.d/default.conf

# Remove the default nginx index
RUN rm -f /var/www/html/index.html 2>/dev/null; \
    rm -f /etc/nginx/http.d/default.conf.bak 2>/dev/null

COPY <<'EOF' /etc/supervisord.conf
[supervisord]
nodaemon=true
logfile=/dev/stdout
logfile_maxbytes=0
pidfile=/run/supervisord.pid

[program:php-fpm]
command=php-fpm --nodaemonize
autostart=true
autorestart=true
stdout_logfile=/dev/stdout
stdout_logfile_maxbytes=0
stderr_logfile=/dev/stderr
stderr_logfile_maxbytes=0

[program:nginx]
command=nginx -g "daemon off;"
autostart=true
autorestart=true
stdout_logfile=/dev/stdout
stdout_logfile_maxbytes=0
stderr_logfile=/dev/stderr
stderr_logfile_maxbytes=0
EOF

EXPOSE 80

CMD ["supervisord", "-c", "/etc/supervisord.conf"]
