FROM jboss/keycloak

USER root

# Install sudo and utils to configure jboss user
RUN microdnf update -y && \
    microdnf install -y sudo shadow-utils passwd && \
    microdnf clean all

# 'Fix' jboss user, add to sudoers
RUN usermod --password jboss jboss && \
    usermod -aG wheel jboss && \
    sed -i 's/# includedir/includedir/' /etc/sudoers && \
    echo "jboss        ALL=(ALL)       NOPASSWD: ALL" >> /etc/sudoers.d/jboss

ADD ./add-to-truststore.sh /opt/jboss/startup-scripts/add-to-truststore.sh
RUN chmod +x /opt/jboss/startup-scripts/add-to-truststore.sh

USER jboss