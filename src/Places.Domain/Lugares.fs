namespace Places.Domain

type LugarId = int
type ComentarioId = int

type PlaceImages = {
    Thumb  : string
    Medium : string
    Full   : string
}

type Lugar = {
    Id          : LugarId
    Nombre      : string
    Descripcion : string
    Rating      : int
    Images      : PlaceImages
}

type Comentario = {
    Id              : ComentarioId
    UserName        : string
    Summary         : string
    Stars           : int
    CommentText     : string
    ProfileImageUrl : string
}

type NuevoComentario = {
    Texto     : string
    Estrellas : int
}
