#!/usr/bin/env bash

CACERTS=$(readlink -e $(dirname $(readlink -e $(which keytool)))"/../lib/security/cacerts")

sudo keytool \
    -import -trustcacerts \
    -alias "dev.local.crt" -file /tmp/dev.local.crt \
    -keystore ${CACERTS} \
    -storepass changeit \
    -noprompt