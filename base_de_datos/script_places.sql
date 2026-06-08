DROP TABLE IF EXISTS favoritos;

DROP TABLE IF EXISTS fotos;

DROP TABLE IF EXISTS comentarios;

DROP TABLE IF EXISTS horarios;

DROP TABLE IF EXISTS lugares;

DROP TABLE IF EXISTS privilegios;

DROP TABLE IF EXISTS cuentas;

DROP TABLE IF EXISTS Usuarios;

DROP TABLE IF EXISTS funcionalidades;

DROP TABLE IF EXISTS roles;

DROP TABLE IF EXISTS Personas;

-- Tabla: Personas
CREATE TABLE Personas (
    id_persona INTEGER NOT NULL,
    nombres VARCHAR(100) NOT NULL,
    primer_apellido VARCHAR(100) NOT NULL,
    segundo_apellido VARCHAR(100) NOT NULL,
    CI INTEGER NOT NULL,
    complemento VARCHAR(2) NOT NULL,
    fecha_nacimiento DATE NOT NULL,
    genero VARCHAR(50) NOT NULL,
    direccion VARCHAR(200) NOT NULL,
    telefono_fijo INTEGER NOT NULL,
    celular INTEGER NOT NULL,
    email VARCHAR(100) NOT NULL,
    CONSTRAINT pk_personas PRIMARY KEY (id_persona)
);

-- Tabla: Roles
CREATE TABLE roles (
    id_rol INTEGER NOT NULL,
    nombre VARCHAR(100) NOT NULL,
    CONSTRAINT pk_roles PRIMARY KEY (id_rol)
);

-- Tabla: Usuarios (Corregido: Relación 1:1 con Personas, ON DELETE CASCADE)
CREATE TABLE Usuarios (
    id_persona INTEGER NOT NULL,
    usuario VARCHAR(50) NOT NULL,
    contrasena VARCHAR(300) NOT NULL, -- Renombrado de contraseña para evitar problemas con la eñe y comillas
    CONSTRAINT pk_usuarios PRIMARY KEY (id_persona),
    CONSTRAINT ak_usuarios_usuario UNIQUE (usuario),
    CONSTRAINT fk_usuarios_personas FOREIGN KEY (id_persona) REFERENCES Personas (id_persona) ON DELETE CASCADE
);

-- Tabla: cuentas (usuario ↔ rol)
CREATE TABLE cuentas (
    id_persona INTEGER NOT NULL,
    id_rol INTEGER NOT NULL,
    CONSTRAINT pk_cuentas PRIMARY KEY (id_persona, id_rol),
    CONSTRAINT fk_cuentas_usuarios FOREIGN KEY (id_persona) REFERENCES Usuarios (id_persona) ON DELETE CASCADE,
    CONSTRAINT fk_cuentas_roles FOREIGN KEY (id_rol) REFERENCES roles (id_rol) ON DELETE CASCADE
);

-- Tabla: Funcionalidades
CREATE TABLE funcionalidades (
    id_func INTEGER NOT NULL,
    nombre VARCHAR(150) NOT NULL,
    CONSTRAINT pk_funcionalidades PRIMARY KEY (id_func),
    CONSTRAINT ak_funcionalidades_nombre UNIQUE (nombre)
);

-- Tabla: privilegios (rol ↔ funcionalidad)
CREATE TABLE privilegios (
    id_rol INTEGER NOT NULL,
    id_func INTEGER NOT NULL,
    CONSTRAINT pk_privilegios PRIMARY KEY (id_rol, id_func),
    CONSTRAINT fk_privilegios_roles FOREIGN KEY (id_rol) REFERENCES roles (id_rol) ON DELETE CASCADE,
    CONSTRAINT fk_privilegios_func FOREIGN KEY (id_func) REFERENCES funcionalidades (id_func) ON DELETE CASCADE
);

-- Tabla: lugares (Corregido: SERIAL PRIMARY KEY)
CREATE TABLE lugares (
    id_lugar SERIAL PRIMARY KEY,
    nombre VARCHAR(100) NOT NULL,
    descripcion TEXT,
    latitud DECIMAL(9, 6),
    longitud DECIMAL(9, 6),
    ubicacion VARCHAR(300),
    municipio VARCHAR(100),
    provincia VARCHAR(100),
    departamento VARCHAR(100),
    url VARCHAR(200)
);

-- Tabla: horarios (Corregido: AUTO_INCREMENT -> SERIAL, y referencias a id_lugar)
CREATE TABLE horarios (
    id SERIAL PRIMARY KEY,
    lugar_id INT NOT NULL,
    dia SMALLINT, -- 1=Lunes, 7=Domingo
    hora_apertura TIME,
    hora_cierre TIME,
    CONSTRAINT fk_horarios_lugar FOREIGN KEY (lugar_id) REFERENCES lugares (id_lugar) ON DELETE CASCADE
);

-- Tabla: comentarios (Corregido: Creado ANTES de fotos, y AUTO_INCREMENT -> SERIAL)
CREATE TABLE comentarios (
    comentario_id SERIAL PRIMARY KEY,
    comentario VARCHAR(500) NOT NULL,
    fecha_com DATE NOT NULL,
    persona_id INT,
    lugar_id INT,
    recomentario_id INT,
    CONSTRAINT comentarios_Usuarios_FK FOREIGN KEY (persona_id) REFERENCES Usuarios (id_persona) ON DELETE SET NULL,
    CONSTRAINT comentarios_Lugares_FK FOREIGN KEY (lugar_id) REFERENCES lugares (id_lugar) ON DELETE CASCADE,
    CONSTRAINT comentarios_recomentario_FK FOREIGN KEY (recomentario_id) REFERENCES comentarios (comentario_id) ON DELETE SET NULL
);

-- Tabla: fotos (Corregido: Creado DESPUÉS de comentarios, y AUTO_INCREMENT -> SERIAL)
CREATE TABLE fotos (
    id_foto SERIAL PRIMARY KEY,
    lugar_id INT,
    url VARCHAR(300) NOT NULL,
    descripcion TEXT,
    comentario_id INT,
    CONSTRAINT fk_fotos_lugar FOREIGN KEY (lugar_id) REFERENCES lugares (id_lugar) ON DELETE CASCADE,
    CONSTRAINT fk_fotos_comentario FOREIGN KEY (comentario_id) REFERENCES comentarios (comentario_id) ON DELETE SET NULL
);

-- Tabla: favoritos
CREATE TABLE favoritos (
    persona_id INT,
    lugar_id INT,
    PRIMARY KEY (persona_id, lugar_id),
    CONSTRAINT favoritos_persona_FK FOREIGN KEY (persona_id) REFERENCES Usuarios (id_persona) ON DELETE CASCADE,
    CONSTRAINT favoritos_lugar_FK FOREIGN KEY (lugar_id) REFERENCES lugares (id_lugar) ON DELETE CASCADE
);

-- ==========================================
-- DATOS SEMILLA (Coincidentes con el Frontend)
-- ==========================================

-- 1. Insertar Roles y Funcionalidades del modelo RBAC
INSERT INTO
    roles (id_rol, nombre)
VALUES (1, 'Administrador'),
    (2, 'Usuario Común');

INSERT INTO
    funcionalidades (id_func, nombre)
VALUES (1, 'Autenticacion.Login'),
    (2, 'Lugares.Crear'),
    (3, 'Lugares.Ver'),
    (4, 'Comentarios.Crear');

-- Privilegios para Administrador (Todo)
INSERT INTO
    privilegios (id_rol, id_func)
VALUES (1, 1),
    (1, 2),
    (1, 3),
    (1, 4),
    -- Privilegios para Usuario Común (Login, Ver Lugares, Crear Comentarios)
    (2, 1),
    (2, 3),
    (2, 4);

-- 2. Insertar Personas
-- Dan (Administrador)
INSERT INTO
    Personas (
        id_persona,
        nombres,
        primer_apellido,
        segundo_apellido,
        CI,
        complemento,
        fecha_nacimiento,
        genero,
        direccion,
        telefono_fijo,
        celular,
        email
    )
VALUES (
        1,
        'Dan',
        'Dev',
        'Admin',
        1234567,
        'LP',
        '1995-05-15',
        'Masculino',
        'Calle Falsa 123',
        2223344,
        7778899,
        'dan@places.com'
    );

-- Margarita Lucresia (Usuario)
INSERT INTO
    Personas (
        id_persona,
        nombres,
        primer_apellido,
        segundo_apellido,
        CI,
        complemento,
        fecha_nacimiento,
        genero,
        direccion,
        telefono_fijo,
        celular,
        email
    )
VALUES (
        2,
        'Margarita',
        'Lucresia',
        'Gomez',
        9876543,
        'CB',
        '1992-08-20',
        'Femenino',
        'Av. America',
        4443322,
        6667788,
        'margarita@places.com'
    );

-- Clara Pettoruti
INSERT INTO
    Personas (
        id_persona,
        nombres,
        primer_apellido,
        segundo_apellido,
        CI,
        complemento,
        fecha_nacimiento,
        genero,
        direccion,
        telefono_fijo,
        celular,
        email
    )
VALUES (
        3,
        'Clara',
        'Pettoruti',
        'Lopez',
        8765432,
        'SC',
        '1994-03-12',
        'Femenino',
        'Equipetrol',
        3332211,
        7776655,
        'clara@places.com'
    );

-- Pancho Villazón
INSERT INTO
    Personas (
        id_persona,
        nombres,
        primer_apellido,
        segundo_apellido,
        CI,
        complemento,
        fecha_nacimiento,
        genero,
        direccion,
        telefono_fijo,
        celular,
        email
    )
VALUES (
        4,
        'Pancho',
        'Villazón',
        'Mamani',
        7654321,
        'OR',
        '1989-11-05',
        'Masculino',
        'Zona Sud',
        5554433,
        6665544,
        'pancho@places.com'
    );

-- Albertina
INSERT INTO
    Personas (
        id_persona,
        nombres,
        primer_apellido,
        segundo_apellido,
        CI,
        complemento,
        fecha_nacimiento,
        genero,
        direccion,
        telefono_fijo,
        celular,
        email
    )
VALUES (
        5,
        'Albertina',
        'Rios',
        'Vaca',
        6543210,
        'BE',
        '1996-01-25',
        'Femenino',
        'Plaza Principal',
        4445566,
        7771122,
        'albertina@places.com'
    );

-- Linda Suarez
INSERT INTO
    Personas (
        id_persona,
        nombres,
        primer_apellido,
        segundo_apellido,
        CI,
        complemento,
        fecha_nacimiento,
        genero,
        direccion,
        telefono_fijo,
        celular,
        email
    )
VALUES (
        6,
        'Linda',
        'Suarez',
        'Mendoza',
        5432109,
        'TJ',
        '1998-07-30',
        'Femenino',
        'Barrio Central',
        6667788,
        7775544,
        'linda@places.com'
    );

-- 3. Insertar Usuarios
-- Contraseña de Dan es: "pass123"
-- BCrypt Hash: $2a$11$/c.5RSW2jTHfGnBe2RZ.XORuYqXE3g6x/RXIHPtKlcsT2jzeynFvm
INSERT INTO
    Usuarios (
        id_persona,
        usuario,
        contrasena
    )
VALUES (
        1,
        'dan@places.com',
        '$2a$11$/c.5RSW2jTHfGnBe2RZ.XORuYqXE3g6x/RXIHPtKlcsT2jzeynFvm'
    );

-- Contraseña de Margarita es: "marga123"
-- BCrypt Hash Real: $2a$11$NdHV5wrdXUaSwoIoVpQJD.pEYYjOHRxHOX7j4BcWsC/FqkQfFtXzK
INSERT INTO
    Usuarios (
        id_persona,
        usuario,
        contrasena
    )
VALUES (
        2,
        'margarita@places.com',
        '$2a$11$NdHV5wrdXUaSwoIoVpQJD.pEYYjOHRxHOX7j4BcWsC/FqkQfFtXzK'
    );

-- Contraseña de Clara es: "cla123"
-- BCrypt Hash Real: $2a$11$R8PoguEilySFuPYmfyj1yerKdtvifUlpqnI2C8IGN7lC36CJ2tGUG
INSERT INTO
    Usuarios (
        id_persona,
        usuario,
        contrasena
    )
VALUES (
        3,
        'clara@places.com',
        '$2a$11$R8PoguEilySFuPYmfyj1yerKdtvifUlpqnI2C8IGN7lC36CJ2tGUG'
    );

-- Contraseña de Pancho es: "pan123"
-- BCrypt Hash Real: $2a$11$FraQDg05/jWbZL3.PXsTre1yEVuBBi576rMGNp8M4djauJB89VLx.
INSERT INTO
    Usuarios (
        id_persona,
        usuario,
        contrasena
    )
VALUES (
        4,
        'pancho@places.com',
        '$2a$11$FraQDg05/jWbZL3.PXsTre1yEVuBBi576rMGNp8M4djauJB89VLx.'
    );

-- Contraseña de Albertina es: "alb123"
-- BCrypt Hash Real: $2a$11$8LmPJOgv9FbNUM9/2fDBl.d8NeO6ceh6RdTNFBA/cfY10V9VL/5am
INSERT INTO
    Usuarios (
        id_persona,
        usuario,
        contrasena
    )
VALUES (
        5,
        'albertina@places.com',
        '$2a$11$8LmPJOgv9FbNUM9/2fDBl.d8NeO6ceh6RdTNFBA/cfY10V9VL/5am'
    );

-- Contraseña de Linda es: "lin123"
-- BCrypt Hash Real: $2a$11$Mgr1h0dL9.MC2sX30xKkEen4SLmEZSMHN6HhcI/9nZNUJlCdL6q8O
INSERT INTO
    Usuarios (
        id_persona,
        usuario,
        contrasena
    )
VALUES (
        6,
        'linda@places.com',
        '$2a$11$Mgr1h0dL9.MC2sX30xKkEen4SLmEZSMHN6HhcI/9nZNUJlCdL6q8O'
    );

-- 4. Asignar Cuentas (Roles)
INSERT INTO
    cuentas (id_persona, id_rol)
VALUES (1, 1), -- Dan -> Admin
    (2, 2), -- Margarita -> Usuario
    (3, 2), -- Clara -> Usuario
    (4, 2), -- Pancho -> Usuario
    (5, 2), -- Albertina -> Usuario
    (6, 2);
-- Linda -> Usuario

-- 5. Insertar los lugares de prueba
INSERT INTO
    lugares (
        id_lugar,
        nombre,
        descripcion,
        latitud,
        longitud,
        ubicacion,
        municipio,
        provincia,
        departamento,
        url
    )
VALUES (
        1,
        'Uyuni',
        'Es un lugar blanco y salado. Es el mayor desierto de sal continuo y alto del mundo, con una superficie de 10 582 km².',
        -20.1338,
        -67.4891,
        'Suroeste de Bolivia',
        'Uyuni',
        'Antonio Quijarro',
        'Potosí',
        'https://es.wikipedia.org/wiki/Salar_de_Uyuni'
    ),
    (
        2,
        'Lago Titicaca',
        'El lago navegable más alto del mundo, cuna de la civilización inca.',
        -16.275,
        -69.091,
        'Frontera con Perú',
        'Copacabana',
        'Manco Kapac',
        'La Paz',
        'https://es.wikipedia.org/wiki/Lago_Titicaca'
    ),
    (
        3,
        'Tiwanaku',
        'Antigua ciudad arqueológica, uno de los centros preincaicos más importantes.',
        -16.555,
        -68.673,
        'Cerca del Lago Titicaca',
        'Tiahuanaco',
        'Ingavi',
        'La Paz',
        'https://es.wikipedia.org/wiki/Tiahuanaco'
    ),
    (
        4,
        'Parque Nacional Madidi',
        'Una de las reservas más ricas en biodiversidad del planeta.',
        -14.283,
        -68.866,
        'Amazonía Boliviana',
        'San Buenaventura',
        'Abel Iturralde',
        'La Paz',
        'https://es.wikipedia.org/wiki/Parque_nacional_Madidi'
    ),
    (
        5,
        'Samaipata',
        'Fuerte preincaico en lo alto de una montaña con vistas espectaculares.',
        -18.175,
        -63.823,
        'Valles cruceños',
        'Samaipata',
        'Florida',
        'Santa Cruz',
        'https://es.wikipedia.org/wiki/Samaipata'
    );

-- 6. Insertar Comentarios del Frontend (conectados a las personas correspondientes)
INSERT INTO
    comentarios (
        comentario_id,
        comentario,
        fecha_com,
        persona_id,
        lugar_id,
        recomentario_id
    )
VALUES (
        1,
        'Medió ganas de quedarme, yo vuelvo! :D',
        '2026-04-20',
        2,
        1,
        NULL
    ),
    (
        2,
        'Fui a ese lugar...pero no recuerdo mucho',
        '2026-04-21',
        3,
        1,
        NULL
    ),
    (
        3,
        'Bastante recomendable...',
        '2026-04-22',
        4,
        1,
        NULL
    ),
    (
        4,
        'Qué lugar más descuidado! no volveré!',
        '2026-04-23',
        5,
        1,
        NULL
    ),
    (
        5,
        'Bastante interesante!',
        '2026-04-24',
        6,
        1,
        NULL
    );

-- 7. Insertar Fotos asociadas a los comentarios
INSERT INTO
    fotos (
        id_foto,
        lugar_id,
        url,
        descripcion,
        comentario_id
    )
VALUES (
        1,
        1,
        'assets/images/persona1.png',
        'Foto de Margarita',
        1
    ),
    (
        2,
        1,
        'assets/images/persona2.jpg',
        'Foto de Clara',
        2
    ),
    (
        3,
        1,
        'assets/images/persona3.jpg',
        'Foto de Pancho',
        3
    ),
    (
        4,
        1,
        'assets/images/persona4.jpg',
        'Foto de Albertina',
        4
    ),
    (
        5,
        1,
        'assets/images/persona5.jpg',
        'Foto de Linda',
        5
    );

-- 8. Insertar fotos principales de los lugares (lugar1.jpg a lugar5.jpg)
INSERT INTO
    fotos (
        id_foto,
        lugar_id,
        url,
        descripcion,
        comentario_id
    )
VALUES (
        6,
        1,
        'assets/images/lugar1.jpg',
        'Vista de Uyuni',
        NULL
    ),
    (
        7,
        2,
        'assets/images/lugar2.jpg',
        'Vista del Lago Titicaca',
        NULL
    ),
    (
        8,
        3,
        'assets/images/lugar3.jpg',
        'Ruinas de Tiwanaku',
        NULL
    ),
    (
        9,
        4,
        'assets/images/lugar4.jpg',
        'Flora y fauna del Madidi',
        NULL
    ),
    (
        10,
        5,
        'assets/images/lugar5.jpg',
        'Fuerte de Samaipata',
        NULL
    );