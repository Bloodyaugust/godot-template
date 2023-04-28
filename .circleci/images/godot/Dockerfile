FROM ubuntu:xenial

ENV GODOT_VERSION "4.0.2"
ENV GODOT_BETA_VERSION "stable"

RUN apt-get update && apt-get install -y --no-install-recommends \
    ca-certificates \
    git \
    ssh \
    tar \
    gzip \
    wget \
    unzip \
    zip \
    python \
    python-openssl \
    && rm -rf /var/lib/apt/lists/*

RUN wget --quiet https://broth.itch.ovh/butler/linux-amd64/LATEST/archive/default -O butler.zip \
    && unzip butler.zip -d /butler \
    && chmod +x /butler/butler

ENV PATH="/butler:${PATH}"

RUN wget --quiet https://downloads.tuxfamily.org/godotengine/${GODOT_VERSION}/Godot_v${GODOT_VERSION}-${GODOT_BETA_VERSION}_linux.x86_64.zip \
    && wget --quiet https://downloads.tuxfamily.org/godotengine/${GODOT_VERSION}/Godot_v${GODOT_VERSION}-${GODOT_BETA_VERSION}_export_templates.tpz \
    && mkdir ~/.cache \
    && mkdir -p ~/.config/godot \
    && mkdir -p ~/.local/share/godot/export_templates/${GODOT_VERSION}.${GODOT_BETA_VERSION} \
    && unzip Godot_v${GODOT_VERSION}-${GODOT_BETA_VERSION}_linux.x86_64.zip \
    && mv Godot_v${GODOT_VERSION}-${GODOT_BETA_VERSION}_linux.x86_64 /usr/local/bin/godot \
    && unzip Godot_v${GODOT_VERSION}-${GODOT_BETA_VERSION}_export_templates.tpz \
    && mv templates/* ~/.local/share/godot/export_templates/${GODOT_VERSION}.${GODOT_BETA_VERSION} \
    && rm -f Godot_v${GODOT_VERSION}-${GODOT_BETA_VERSION}_export_templates.tpz Godot_v${GODOT_VERSION}-${GODOT_BETA_VERSION}_linux.x86_64.zip \
    && cat ~/.local/share/godot/export_templates/${GODOT_VERSION}.${GODOT_BETA_VERSION}/version.txt
