FROM nginx

ENV SSL_CERT=/tmp/dev.local.crt
ENV SSL_KEY=/tmp/dev.local.key

ENV KEYCLOAK_URI=http://keycloak:8080
ENV STEAMIDP_URI=http://steamidp:80

ADD ./proxy_ssl.conf.template /etc/nginx/templates/