# Guia d'instal·lació - AssistenciaPlus

## Requisits del servidor

| Component | Versió mínima |
|-----------|--------------|
| Ubuntu | 22.04 LTS o superior |
| Docker | 24.0+ |
| Docker Compose | 2.20+ |
| RAM | 4 GB (8 GB recomanat amb Ollama) |
| Disc | 20 GB lliures |
| CPU | 2 cores (4+ amb Ollama) |

---

## 1. Preparació del servidor Ubuntu

```bash
# Actualitzar el sistema
sudo apt update && sudo apt upgrade -y

# Instal·lar Docker
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh
sudo usermod -aG docker $USER
newgrp docker

# Verificar instal·lació
docker --version
docker compose version
```

---

## 2. Clonar el repositori

```bash
# Crear directori de desplegament
sudo mkdir -p /docker/AssistenciaPlus
sudo chown $USER:$USER /docker/AssistenciaPlus

# Clonar
cd /docker/AssistenciaPlus
git clone https://github.com/TU_USUARI/AssistenciaPlus.git .
```

---

## 3. Configurar l'entorn

```bash
# Copiar la plantilla
cp .env.example .env

# Editar amb els valors reals
nano .env
```

**Valors importants a canviar al `.env`:**

| Variable | Descripció |
|----------|-----------|
| `APP_PUBLIC_URL` | URL pública (p.ex. `https://tuteapps.ddns.net:4446`) |
| `DB_PASSWORD` | Contrasenya forta per PostgreSQL |
| `REDIS_PASSWORD` | Contrasenya forta per Redis |
| `JWT_SECRET` | Clau secreta JWT (mínim 32 caràcters) |
| `SMTP_USER` | Compte Gmail |
| `SMTP_PASSWORD` | App password de Gmail |

---

## 4. Configurar certificats SSL

```bash
# Crear directori de certificats
mkdir -p nginx/certs

# Opció A: Copiar certificats existents
cp /ruta/al/teu/fullchain.pem nginx/certs/
cp /ruta/al/teu/privkey.pem nginx/certs/

# Establir permisos correctes
chmod 600 nginx/certs/privkey.pem
chmod 644 nginx/certs/fullchain.pem
```

---

## 5. Primer desplegament

```bash
cd /docker/AssistenciaPlus

# Construir i aixecar tots els serveis
docker compose up -d --build

# Verificar que tots els contenidors estan actius
docker compose ps

# Veure logs de l'API (les migracions s'apliquen automàticament)
docker compose logs -f api
```

**Sortida esperada:**
```
assistenciaplus_nginx    Up
assistenciaplus_web      Up
assistenciaplus_api      Up (healthy)
assistenciaplus_db       Up (healthy)
assistenciaplus_redis    Up (healthy)
assistenciaplus_ollama   Up
```

---

## 6. Configurar Ollama (IA local) - opcional

```bash
# Descarregar el model (pot trigar uns minuts)
docker exec assistenciaplus_ollama ollama pull llama3.2

# Verificar que el model s'ha descarregat
docker exec assistenciaplus_ollama ollama list
```

> **Nota**: Sense Ollama, totes les funcionalitats funcionen excepte les consultes en llenguatge natural.

---

## 7. Crear el primer usuari (Equip Directiu)

Accedeix a la base de dades per crear el primer usuari administrador:

```bash
# Accedir al contenidor de PostgreSQL
docker exec -it assistenciaplus_db psql -U appuser -d assistenciaplus

# Inserir usuari administrador (canvia els valors!)
INSERT INTO "Users" ("Id", "FirstName", "LastName", "Email", "PasswordHash", "Role",
                     "Language", "IsActive", "CreatedAt", "IsDeleted")
VALUES (
    gen_random_uuid(),
    'Admin',
    'Principal',
    'admin@escola.cat',
    -- Hash de 'Admin1234!' generat amb BCrypt (work factor 12)
    '$2a$12$EXEMPLE_HASH_AQUI',
    2,  -- Management
    'ca',
    true,
    NOW(),
    false
);

\q
```

> **Alternativa**: Un cop la API estigui en marxa, pots usar l'endpoint `POST /api/auth/register-initial` (disponible únicament si no hi ha cap usuari de rol Management a la base de dades).

---

## 8. Verificació

```bash
# Comprovar l'API
curl -k https://localhost:4446/health

# Comprovar logs
docker compose logs --tail=50 api
docker compose logs --tail=20 nginx
```

Accedeix a: **https://tuteapps.ddns.net:4446**

---

## 9. Actualitzar l'aplicació

```bash
cd /docker/AssistenciaPlus

# Obtenir els darrers canvis
git pull origin main

# Reconstruir i reiniciar (sense temps d'inactivitat)
docker compose up -d --build --no-deps api web

# Netejar imatges antigues
docker image prune -f
```

---

## 10. Còpies de seguretat

### Backup de la base de dades

```bash
# Crear script de backup automàtic
cat > /docker/AssistenciaPlus/scripts/backup.sh << 'EOF'
#!/bin/bash
BACKUP_DIR="/docker/backups/assistenciaplus"
DATE=$(date +%Y%m%d_%H%M%S)
mkdir -p $BACKUP_DIR

docker exec assistenciaplus_db pg_dump \
    -U appuser assistenciaplus \
    | gzip > $BACKUP_DIR/db_$DATE.sql.gz

# Mantenir últims 30 backups
find $BACKUP_DIR -name "db_*.sql.gz" -mtime +30 -delete

echo "Backup completat: $BACKUP_DIR/db_$DATE.sql.gz"
EOF

chmod +x /docker/AssistenciaPlus/scripts/backup.sh

# Programar backup diari a les 2:00 AM
echo "0 2 * * * /docker/AssistenciaPlus/scripts/backup.sh" | crontab -
```

### Restaurar backup

```bash
# Restaurar des d'un fitxer de backup
gunzip -c /docker/backups/assistenciaplus/db_20241201_020000.sql.gz \
    | docker exec -i assistenciaplus_db psql -U appuser assistenciaplus
```

---

## 11. Monitoratge

```bash
# Veure estat de tots els contenidors
docker compose ps

# Veure ús de recursos en temps real
docker stats

# Logs en temps real
docker compose logs -f

# Logs d'un servei específic
docker compose logs -f api
```

---

## Resolució de problemes

### La base de dades no arrenca

```bash
docker compose logs db
# Comprovar que el volum postgres_data no té problemes de permisos
docker volume inspect assistenciaplus_postgres_data
```

### L'API no es connecta a Redis

```bash
docker compose logs redis
# Verificar que la contrasenya al .env és correcta
docker exec assistenciaplus_redis redis-cli -a $REDIS_PASSWORD ping
```

### Certificat SSL no vàlid

```bash
# Verificar els fitxers de certificat
ls -la nginx/certs/
# Comprovar que el nom de domini coincideix
openssl x509 -in nginx/certs/fullchain.pem -text -noout | grep "Subject:"
```

### Errors de migració

```bash
# Veure logs detallats de l'API a l'inici
docker compose logs api | head -100
# Reiniciar únicament l'API
docker compose restart api
```

---

## Informació de ports

| Servei | Port intern | Port extern |
|--------|------------|------------|
| Nginx (HTTPS) | 443 | 4446 |
| Nginx (HTTP redirect) | 80 | 80 |
| API | 8080 | (intern) |
| Web | 8080 | (intern) |
| PostgreSQL | 5432 | (intern) |
| Redis | 6379 | (intern) |
| Ollama | 11434 | (intern) |

> Els serveis interns no són accessibles des de fora del servidor (xarxa Docker interna).
