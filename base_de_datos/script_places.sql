
DROP TABLE IF EXISTS privilegios;
DROP TABLE IF EXISTS cuentas;
DROP TABLE IF EXISTS Usuarios;
DROP TABLE IF EXISTS funcionalidades;
DROP TABLE IF EXISTS roles;
DROP TABLE IF EXISTS Personas;
DROP TABLE IF EXISTS horarios;
DROP TABLE IF EXISTS fotos;
DROP TABLE IF EXISTS lugares;
DROP TABLE IF EXISTS comentarios;

-- Tabla: Personas
CREATE TABLE Personas (
    id_persona  INTEGER     NOT NULL,
    nombres     VARCHAR(100) NOT NULL,
    primer_apellido  VARCHAR(100) NOT NULL,
    segundo_apellido VARCHAR(100) NOT NULL,
    CI          INTEGER     NOT NULL,
    complemento VARCHAR(2)  NOT NULL,
    fecha_nacimiento DATE   NOT NULL,
    genero      VARCHAR(50) NOT NULL,
    direccion   VARCHAR(200) NOT NULL,
    telefono_fijo INTEGER   NOT NULL,
    celular     INTEGER     NOT NULL,
    email       VARCHAR(100) NOT NULL,
    CONSTRAINT pk_personas PRIMARY KEY (id_persona)
);

-- Tabla: Roles
CREATE TABLE roles (
    id_rol  INTEGER      NOT NULL,
    nombre  VARCHAR(100) NOT NULL,
    CONSTRAINT pk_roles PRIMARY KEY (id_rol)
);

-- Tabla: Usuarios
CREATE TABLE Usuarios (
    id_persona  INTEGER     NOT NULL,
    usuario     VARCHAR(50) NOT NULL,
    contraseña  VARCHAR(300) NOT NULL,
    CONSTRAINT pk_usuarios PRIMARY KEY (id_persona),
    CONSTRAINT ak_usuarios_usuario UNIQUE (usuario),
    CONSTRAINT fk_usuarios_personas FOREIGN KEY (id_persona)
        REFERENCES Personas (id_persona)
);

-- Tabla: cuentas (usuario ↔ rol)
CREATE TABLE cuentas (
    id_persona  INTEGER NOT NULL,
    id_rol      INTEGER NOT NULL,
    CONSTRAINT pk_cuentas PRIMARY KEY (id_persona, id_rol),
    CONSTRAINT fk_cuentas_usuarios FOREIGN KEY (id_persona)
        REFERENCES Usuarios (id_persona),
    CONSTRAINT fk_cuentas_roles FOREIGN KEY (id_rol)
        REFERENCES roles (id_rol)
);

-- Tabla: Funcionalidades
CREATE TABLE funcionalidades (
    id_func    INTEGER      NOT NULL,
    nombre     VARCHAR(150) NOT NULL,
    CONSTRAINT pk_funcionalidades PRIMARY KEY (id_func),
    CONSTRAINT ak_funcionalidades_nombre UNIQUE (nombre)
);

-- Tabla: privilegios (rol ↔ funcionalidad)
CREATE TABLE privilegios (
    id_rol   INTEGER NOT NULL,
    id_func  INTEGER NOT NULL,
    CONSTRAINT pk_privilegios PRIMARY KEY (id_rol, id_func),
    CONSTRAINT fk_privilegios_roles FOREIGN KEY (id_rol)
        REFERENCES roles (id_rol),
    CONSTRAINT fk_privilegios_func FOREIGN KEY (id_func)
        REFERENCES funcionalidades (id_func)
);

CREATE TABLE lugares (
    id_lugar PRIMARY KEY,
    nombre VARCHAR(100),
    descripcion TEXT,
    latitud DECIMAL(9,6),
    longitud DECIMAL(9,6),
    ubicacion VARCHAR(300),
    municipio VARCHAR(100),
    provincia VARCHAR(100),
    departamento VARCHAR(100),
    url varchar(200)
);

CREATE TABLE horarios (
    id INT AUTO_INCREMENT PRIMARY KEY,
    lugar_id INT REFERENCES lugares(id),
    dia SMALLINT, -- 1=Lunes, 7=Domingo
    hora_apertura TIME,
    hora_cierre TIME,
    CONSTRAINT fk_horarios_lugar FOREIGN KEY (lugar_id)
        REFERENCES lugares(id_lugar)
        ON DELETE CASCADE
);

CREATE TABLE fotos (
    id_foto INT AUTO_INCREMENT,
    lugar_id INT,
    -- nombre VARCHAR(100),
    url VARCHAR(300),
    descripcion TEXT,
    comentario_id INT,
    CONSTRAINT pk_fotos PRIMARY KEY (id_foto),
    CONSTRAINT fk_fotos_lugar FOREIGN KEY (lugar_id) REFERENCES lugares(id_lugar),
    CONSTRAINT fk_fotos_comentario FOREIGN KEY (comentario_id) REFERENCES comentarios(comentario_id)
);

CREATE TABLE comentarios (
    comentario_id INT AUTO_INCREMENT PRIMARY KEY,
    comentario VARCHAR(500) NOT NULL,
    fecha_com DATE NOT NULL,
    persona_id INT,
    lugar_id INT,
    recomentario_id INT,
    CONSTRAINT comentarios_Usuarios_FK FOREIGN KEY (persona_id) REFERENCES Usuarios(id_persona),
    CONSTRAINT comentarios_Lugares_FK FOREIGN KEY (lugar_id) REFERENCES Lugares(id_lugar),
    CONSTRAINT comentarios_recomentario_FK FOREIGN KEY (recomentario_id) REFERENCES  Comentarios(comentario_id)
);

CREATE TABLE favoritos (
    persona_id INT,
    lugar_id INT,
    PRIMARY KEY (persona_id, lugar_id),
    CONSTRAINT favoritos_persona_FK FOREIGN KEY (persona_id) REFERENCES Usuarios(id_persona),
    CONSTRAINT favoritos_lugar_FK FOREIGN KEY (lugar_id) REFERENCES Lugares(id_lugar)
);
