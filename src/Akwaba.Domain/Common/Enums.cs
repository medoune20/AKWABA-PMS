namespace Akwaba.Domain.Common;

/// <summary>Cycle de vie d'un hôtel (tenant) sur la plateforme.</summary>
public enum StatutTenant { EnAttente = 0, Approuve = 1, Refuse = 2, Suspendu = 3 }

/// <summary>État d'un séjour.</summary>
public enum StatutSejour { Reserve = 0, EnCours = 1, Termine = 2, Annule = 3 }

/// <summary>État d'une réservation.</summary>
public enum StatutReservation { Confirmee = 0, Annulee = 1, NoShow = 2, Soldee = 3 }

/// <summary>État d'une note de chambre.</summary>
public enum StatutFolio { Ouvert = 0, Solde = 1, Annule = 2 }

/// <summary>Nature d'une ligne de note.</summary>
public enum CategorieLigne { Hebergement = 0, Restauration = 1, Taxe = 2, Remise = 3, Extra = 4 }

/// <summary>État d'entretien d'une chambre.</summary>
public enum StatutMenage { Propre = 0, Sale = 1, EnCours = 2, HorsService = 3 }

/// <summary>Moyens de paiement (marché ouest-africain).</summary>
public enum MoyenPaiement { Especes = 0, OrangeMoney = 1, MtnMomo = 2, Wave = 3, MoovMoney = 4, Carte = 5 }

/// <summary>État d'un paiement.</summary>
public enum StatutPaiement { EnAttente = 0, Confirme = 1, Echec = 2, Expire = 3 }

/// <summary>État d'une commande restaurant/bar.</summary>
public enum StatutCommande { Ouverte = 0, EnvoyeeSurNote = 1, Payee = 2, Annulee = 3 }
